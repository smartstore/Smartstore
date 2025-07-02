using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Logging;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ActivityLogController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public ActivityLogController(SmartDbContext db, IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
        }

        #region Activity log types

        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public IActionResult ActivityLogTypes()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public async Task<IActionResult> ActivityLogTypesList(GridCommand command)
        {
            var mapper = MapperFactory.GetMapper<ActivityLogType, ActivityLogTypeModel>();
            var models = await _db.ActivityLogTypes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            models = [.. models
                .GroupBy(x => new { x.SystemKeyword, x.Enabled })
                .Select(g => g.First())];

            var gridModel = new GridModel<ActivityLogTypeModel>
            {
                Rows = models,
                Total = models.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Update)]
        public async Task<IActionResult> ActivityLogTypesUpdate(ActivityLogTypeModel model)
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
        public async Task<IActionResult> ActivityLogs()
        {
            var activityLogTypes = await _db.ActivityLogTypes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            activityLogTypes = [.. activityLogTypes
                .GroupBy(x => new { x.SystemKeyword, x.Enabled })
                .Select(g => g.First())];

            var model = new ActivityLogListModel
            {
                ActivityLogTypes = [.. activityLogTypes
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })]
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Read)]
        public async Task<IActionResult> ActivityLogList(GridCommand command, ActivityLogListModel model)
        {
            DateTime? startDateValue = (model.CreatedOnFrom == null) ? null
                : _dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedOnTo == null) ? null
                : _dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

            var query = _db.ActivityLogs
                .Include(x => x.Customer)
                .Include(x => x.ActivityLogType)
                .AsNoTracking();

            if (model.ActivityLogTypeId != 0)
            {
                query = query.Where(x => x.ActivityLogTypeId == model.ActivityLogTypeId);
            }

            var activityLogs = await query
                .ApplyDateFilter(startDateValue, endDateValue)
                .ApplyCustomerFilter(model.CustomerEmail, model.CustomerSystemAccount)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var systemAccountCustomers = await _db.Customers
                .AsNoTracking()
                .Where(x => x.IsSystemAccount)
                .ToDictionaryAsync(x => x.Id);

            var resMap = new Dictionary<string, string>
            {
                { "SearchEngine", T("Admin.System.SystemCustomerNames.SearchEngine") },
                { "BackgroundTask", T("Admin.System.SystemCustomerNames.BackgroundTask") },
                { "PdfConverter", T("Admin.System.SystemCustomerNames.PdfConverter") }
            };

            var mapper = MapperFactory.GetMapper<ActivityLog, ActivityLogModel>();
            var activityLogModels = await activityLogs.SelectAwait(async x =>
            {
                var model = await mapper.MapAsync(x);
                var systemCustomer = systemAccountCustomers.Get(x.CustomerId);

                model.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);

                if (systemCustomer != null)
                {
                    model.IsSystemAccount = true;
                    model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = x.CustomerId, area = "Admin" });

                    if (systemCustomer.IsBot())
                    {
                        model.SystemAccountName = resMap["SearchEngine"];
                    }
                    else if (systemCustomer.IsBackgroundTaskAccount())
                    {
                        model.SystemAccountName = resMap["BackgroundTask"];
                    }
                    else if (systemCustomer.IsPdfConverter())
                    {
                        model.SystemAccountName = resMap["PdfConverter"];
                    }
                    else
                    {
                        model.SystemAccountName = string.Empty;
                    }
                }

                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<ActivityLogModel>
            {
                Rows = activityLogModels,
                Total = await activityLogs.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.ActivityLog.Delete)]
        public async Task<IActionResult> ActivityLogDelete(GridSelection selection)
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
        [Permission(Permissions.Configuration.ActivityLog.Delete)]
        public async Task<IActionResult> DeleteAll()
        {
            await Services.ActivityLogger.ClearAllActivitiesAsync();

            return RedirectToAction(nameof(ActivityLogs));
        }

        #endregion
    }
}
