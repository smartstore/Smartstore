using FluentValidation;
using Smartstore.Admin.Models;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class SettingController : AdminController
    {
        [Permission(Permissions.Configuration.Setting.Read)]
        public IActionResult AllSettings(SettingListModel model)
        {
            model.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Read)]
        public async Task<IActionResult> SettingList(GridCommand command, SettingListModel model)
        {
            var stores = Services.StoreContext.GetAllStores();

            var query = _db.Settings.AsNoTracking();

            if (model.SearchSettingName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchSettingName);
            }

            if (model.SearchSettingValue.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Value, model.SearchSettingValue);
            }

            if (model.SearchStoreId != 0)
            {
                query = query.Where(x => x.StoreId == model.SearchStoreId);
            }

            var settings = await query
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var allStoresStr = T("Admin.Common.StoresAll").Value;
            var allStoreNames = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id, x => x.Name);

            var rows = settings
                .Select(x => new SettingModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Value = x.Value,
                    StoreId = x.StoreId,
                    Store = x.StoreId == 0 ? allStoresStr : allStoreNames.Get(x.StoreId).NaIfEmpty()
                })
                .ToList();

            var gridModel = new GridModel<SettingModel>
            {
                Rows = rows,
                Total = await settings.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Update)]
        public async Task<IActionResult> SettingUpdate(SettingModel model)
        {
            model.Name = model.Name.Trim();

            if (model.Value.HasValue())
            {
                model.Value = model.Value.Trim();
            }
            model.StoreId = model.StoreId.GetValueOrDefault();

            var success = false;
            var setting = await _db.Settings.FindByIdAsync(model.Id);

            if (setting != null)
            {
                await MapperFactory.MapAsync(model, setting);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Create)]
        public async Task<IActionResult> SettingInsert(SettingModel model)
        {
            model.Name = model.Name.Trim();

            if (model.Value.HasValue())
            {
                model.Value = model.Value.Trim();
            }

            var success = true;
            var setting = new Setting();
            await MapperFactory.MapAsync(model, setting);
            _db.Settings.Add(setting);
            await _db.SaveChangesAsync();

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Delete)]
        public async Task<IActionResult> SettingDelete(GridSelection selection)
        {
            var entities = await _db.Settings.GetManyAsync(selection.GetEntityIds(), true);
            if (entities.Count > 0)
            {
                _db.Settings.RemoveRange(entities);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(
                    KnownActivityLogTypes.DeleteSetting,
                    T("ActivityLog.DeleteSetting"),
                    string.Join(", ", entities.Select(x => x.Name)));                
            }

            return Json(new { Success = true, entities.Count });
        }
    }
}
