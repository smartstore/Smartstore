using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Microsoft.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Microsoft.Auth.Controllers
{
    public class MicrosoftAuthController : AdminController
    {
        private readonly IOptionsMonitorCache<MicrosoftAccountOptions> _optionsCache;
        private readonly IProviderManager _providerManager;

        public MicrosoftAuthController(IOptionsMonitorCache<MicrosoftAccountOptions> optionsCache, IProviderManager providerManager)
        {
            _optionsCache = optionsCache;
            _providerManager = providerManager;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(MicrosoftExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<MicrosoftExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-microsoft";

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.Microsoft.Auth").Metadata;

            return View(model);
        }

        [HttpPost, SaveSetting]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public IActionResult Configure(ConfigurationModel model, MicrosoftExternalAuthSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            // TODO: (mh) (core) This must also be called when settings change via all settings grid.
            _optionsCache.TryRemove(MicrosoftAccountDefaults.AuthenticationScheme);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
