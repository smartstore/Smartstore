using Smartstore.Admin.Models.Modularity;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;

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
            return RedirectToAction(nameof(Providers));
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

            var provider = _providerManager.GetProvider<IExternalAuthenticationMethod>(systemName);
            var dirty = provider.IsMethodActive(_externalAuthenticationSettings) != activate;

            if (dirty)
            {
                if (!activate)
                {
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Remove(x => x.EqualsNoCase(provider.Metadata.SystemName));
                }
                else
                {
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Add(provider.Metadata.SystemName);
                }

                await Services.SettingFactory.SaveSettingsAsync(_externalAuthenticationSettings);
                await _widgetService.ActivateWidgetAsync(provider.Metadata.SystemName, activate);
            }

            return RedirectToAction(nameof(Providers));
        }
    }
}
