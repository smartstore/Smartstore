using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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


    }
}
