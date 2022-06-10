using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Google.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Google.Auth.Controllers
{
    [Route("[area]/google/auth/{action=index}/{id?}")]
    public class GoogleAuthController : AdminController
    {
        private readonly IOptionsMonitorCache<GoogleOptions> _optionsCache;
        private readonly IProviderManager _providerManager;

        public GoogleAuthController(IOptionsMonitorCache<GoogleOptions> optionsCache, IProviderManager providerManager)
        {
            _optionsCache = optionsCache;
            _providerManager = providerManager;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(GoogleExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<GoogleExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-google";

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.Google.Auth").Metadata;

            return View(model);
        }

        [HttpPost, SaveSetting]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public IActionResult Configure(ConfigurationModel model, GoogleExternalAuthSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);
            // TODO: (mh) (core) This must also be called when settings change via all settings grid.
            _optionsCache.TryRemove(GoogleDefaults.AuthenticationScheme);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
