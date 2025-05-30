using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
using Smartstore.Klarna.Configuration;
using Smartstore.Klarna.Models;
using Smartstore.Web.Common;
using Smartstore.Web.Common.Controllers;
using Smartstore.Web.Common.Settings;
using System.Threading.Tasks;
using Smartstore.ComponentModel; // Required for ObjectMapping

namespace Smartstore.Klarna.Controllers
{
    [Area(AreaNames.Admin)]
    [Route("admin/klarna/[action]")] // Adjusted route to match typical module admin controllers
    public class KlarnaAdminController : AdminController
    {
        private readonly ISettingService _settingService;

        public KlarnaAdminController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        [Permission(Permissions.Configuration.PaymentMethod.Read)]
        public async Task<IActionResult> Configure()
        {
            var storeScope = await GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingsAsync<KlarnaSettings>(storeScope);
            var model = new KlarnaConfigurationModel();

            ObjectMapping.Map(settings, model);
            model.ActiveStoreScopeConfiguration = storeScope;

            // Prepare multi-store settings if applicable (using _OverrideForStore checkboxes)
            if (storeScope > 0)
            {
                model.ApiKey_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ApiKey, storeScope);
                model.ApiSecret_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ApiSecret, storeScope);
                model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.UseSandbox, storeScope);
                model.Region_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Region, storeScope);
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PaymentMethod.Update)]
        [SaveChanges(typeof(KlarnaSettings))]
        public async Task<IActionResult> Configure(KlarnaConfigurationModel model) // Removed 'bool save, string delete' as SaveChanges handles it
        {
            var storeScope = await GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingsAsync<KlarnaSettings>(storeScope);

            if (!ModelState.IsValid)
            {
                return await Configure();
            }

            ModelState.Clear(); // Important for SaveChangesAttribute
            ObjectMapping.Map(model, settings);

            // Handle multi-store settings overrides
            await _settingService.ApplySettingOverrideAsync(settings, model, x => x.ApiKey, storeScope, model.ApiKey_OverrideForStore);
            await _settingService.ApplySettingOverrideAsync(settings, model, x => x.ApiSecret, storeScope, model.ApiSecret_OverrideForStore);
            await _settingService.ApplySettingOverrideAsync(settings, model, x => x.UseSandbox, storeScope, model.UseSandbox_OverrideForStore);
            await _settingService.ApplySettingOverrideAsync(settings, model, x => x.Region, storeScope, model.Region_OverrideForStore);

            // SaveChangesAttribute will persist the settings.

            NotifySuccess(T("Admin.Configuration.Settings.Updated"));
            return RedirectToAction("Configure");
        }
    }
}
