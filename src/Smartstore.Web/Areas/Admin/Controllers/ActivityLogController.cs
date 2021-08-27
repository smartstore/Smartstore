using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Logging;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ActivityLogController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly AdminAreaSettings _adminAreaSettings;

        public ActivityLogController(
            SmartDbContext db,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            AdminAreaSettings adminAreaSettings)
        {
            _db = db;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _adminAreaSettings = adminAreaSettings;
        }

        #region Activity log types

        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public IActionResult ListTypes()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> ActivityTypesList(GridCommand command)
        {
            var activityLogTypeModels = await _db.ActivityLogTypes
                .AsNoTracking()
                .ApplyGridCommand(command)
                .SelectAsync(async x =>
                {
                    return await MapperFactory.MapAsync<ActivityLogType, ActivityLogTypeModel>(x);
                })
                .AsyncToList();

            var gridModel = new GridModel<ActivityLogTypeModel>
            {
                Rows = activityLogTypeModels,
                Total = activityLogTypeModels.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Update)]
        public async Task<IActionResult> ActivityTypesUpdate(ActivityLogTypeModel model)
        {
            var success = false;

            var activityLogType = await _db.ActivityLogTypes.FindByIdAsync(model.Id);
            if (activityLogType != null)
            {
                activityLogType.Enabled = model.Enabled;
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        #endregion

        #region Activity log

        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public async Task<IActionResult> ListLogs()
        {
            var model = new ActivityLogListModel
            {
                ActivityLogTypes = await _db.ActivityLogTypes
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public async Task<IActionResult> ListActivityLogs(GridCommand command, ActivityLogListModel model)
        {
            DateTime? startDateValue = (model.CreatedOnFrom == null) ? null
                : _dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedOnTo == null) ? null
                : _dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

            var query = _db.ActivityLogs.AsNoTracking();

            if (model.ActivityLogTypeId != 0)
            {
                query = query.Where(x => x.ActivityLogTypeId == model.ActivityLogTypeId);
            }

            var activityLogs = await query
                .ApplyDateFilter(startDateValue, endDateValue)
                .ApplyCustomerFilter(model.CustomerEmail, model.CustomerSystemAccount)
                .ApplyGridCommand(command)
                .Include(x => x.Customer)
                .Include(x => x.ActivityLogType)
                .ToPagedList(command)
                .LoadAsync();

            var systemAccountCustomers = await _db.Customers
                .AsNoTracking()
                .Where(x => x.IsSystemAccount)
                .ToListAsync();

            var activityLogModels = await activityLogs.SelectAsync(async x =>
            {
                var model = await MapperFactory.MapAsync<ActivityLog, ActivityLogModel>(x);
                var systemCustomer = systemAccountCustomers.FirstOrDefault(y => y.Id == x.CustomerId);

                model.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                model.IsSystemAccount = systemCustomer != null;
                model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = x.CustomerId });

                if (systemCustomer != null)
                {
                    if (systemCustomer.IsSearchEngineAccount())
                    {
                        model.SystemAccountName = T("Admin.System.SystemCustomerNames.SearchEngine");
                    }   
                    else if (systemCustomer.IsBackgroundTaskAccount())
                    {
                        model.SystemAccountName = T("Admin.System.SystemCustomerNames.BackgroundTask");
                    }   
                    else if (systemCustomer.IsPdfConverter())
                    {
                        model.SystemAccountName = T("Admin.System.SystemCustomerNames.PdfConverter");
                    }   
                    else
                    {
                        model.SystemAccountName = string.Empty;
                    }
                }

                return model;
            }).AsyncToList();

            var gridModel = new GridModel<ActivityLogModel>
            {
                Rows = activityLogModels,
                Total = await activityLogs.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Delete)]
        public async Task<IActionResult> Delete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var activityLogs = await _db.ActivityLogs.GetManyAsync(ids, true);

                _db.ActivityLogs.RemoveRange(activityLogs);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Delete)]
        public async Task<IActionResult> DeleteAll()
        {
            await _db.ActivityLogs.DeleteAllAsync();
            await _db.SaveChangesAsync();
        
            return RedirectToAction("ListLogs");
        }

        #endregion
    }
}
