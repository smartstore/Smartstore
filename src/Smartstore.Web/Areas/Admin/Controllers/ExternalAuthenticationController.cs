using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Modularity;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Admin.Controllers
{
    public partial class ExternalAuthenticationController : AdminController
    {
        private readonly IWidgetService _widgetService;
        private readonly IProviderManager _providerManager;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly ModuleManager _moduleManager;

        public ExternalAuthenticationController(
            IWidgetService widgetService,
            IProviderManager providerManager,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            ModuleManager moduleManager)
        {
            _widgetService = widgetService;
            _providerManager = providerManager;
            _externalAuthenticationSettings = externalAuthenticationSettings;
            _moduleManager = moduleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Providers");
        }

        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Providers()
        {
            var widgetsModel = new List<AuthenticationMethodModel>();
            var methods = _providerManager.GetAllProviders<IExternalAuthenticationMethod>();

            foreach (var method in methods)
            {
                var model = _moduleManager.ToProviderModel<IExternalAuthenticationMethod, AuthenticationMethodModel>(method);
                model.IsActive = method.IsMethodActive(_externalAuthenticationSettings);
                widgetsModel.Add(model);
            }

            return View(widgetsModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Authentication.Activate)]
        public async Task<IActionResult> ActivateProvider(string systemName, bool activate)
        {
            await _widgetService.ActivateWidgetAsync(systemName, activate);

            var method = _providerManager.GetProvider<IExternalAuthenticationMethod>(systemName);
            bool dirty = method.IsMethodActive(_externalAuthenticationSettings) != activate;
            if (dirty)
            {
                if (!activate)
                {
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Remove(method.Metadata.SystemName);
                }
                else
                {
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Add(method.Metadata.SystemName);
                }

                await Services.SettingFactory.SaveSettingsAsync(_externalAuthenticationSettings);
                await _widgetService.ActivateWidgetAsync(method.Metadata.SystemName, activate);
            }

            return RedirectToAction("Providers");
        }
    }
}
