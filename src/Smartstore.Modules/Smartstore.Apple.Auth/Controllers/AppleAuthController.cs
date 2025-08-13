using Microsoft.AspNetCore.Mvc;
using Smartstore.Apple.Auth.Models;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Apple.Auth.Controllers
{
    [Route("[area]/apple/auth/{action=index}/{id?}")]
    public class AppleAuthController : AdminController
    {
        private readonly IOptionsMonitorCache<AppleAuthenticationOptions> _optionsCache;
        private readonly IProviderManager _providerManager;

        public AppleAuthController(IOptionsMonitorCache<AppleAuthenticationOptions> optionsCache, IProviderManager providerManager)
        {
            _optionsCache = optionsCache;
            _providerManager = providerManager;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(AppleExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<AppleExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetBaseUrl();
            model.RedirectUrl = $"{host}signin-apple";

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.Apple.Auth").Metadata;

            return View(model);
        }

        [HttpPost, SaveSetting]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public IActionResult Configure(ConfigurationModel model, AppleExternalAuthSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);

            _optionsCache.TryRemove(AppleAuthenticationDefaults.AuthenticationScheme);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}