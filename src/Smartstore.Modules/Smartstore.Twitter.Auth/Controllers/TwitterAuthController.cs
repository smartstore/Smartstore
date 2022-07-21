using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Twitter.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Twitter.Auth.Controllers
{
    public class TwitterAuthController : AdminController
    {
        private readonly IOptionsMonitorCache<TwitterOptions> _optionsCache;
        private readonly IProviderManager _providerManager;

        public TwitterAuthController(IOptionsMonitorCache<TwitterOptions> optionsCache, IProviderManager providerManager)
        {
            _optionsCache = optionsCache;
            _providerManager = providerManager;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(TwitterExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<TwitterExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-twitter";

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.Twitter.Auth").Metadata;

            return View(model);
        }

        [HttpPost, SaveSetting]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public IActionResult Configure(ConfigurationModel model, TwitterExternalAuthSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);
            // TODO: (mh) (core) This must also be called when settings change via all settings grid.
            _optionsCache.TryRemove(TwitterDefaults.AuthenticationScheme);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
