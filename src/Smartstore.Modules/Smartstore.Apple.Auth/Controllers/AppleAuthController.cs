using System;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using Smartstore.Apple.Auth.Models;
using Smartstore.Caching;
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
        private readonly static Lazy<bool> _shouldEnableIISUserProfile = new(ShouldEnableIISUserProfile);

        private readonly IOptionsMonitorCache<AppleAuthenticationOptions> _optionsCache;
        private readonly IProviderManager _providerManager;
        private readonly ICacheManager _cache;

        public AppleAuthController(
            IOptionsMonitorCache<AppleAuthenticationOptions> optionsCache, 
            IProviderManager providerManager,
            ICacheManager cache)
        {
            _optionsCache = optionsCache;
            _providerManager = providerManager;
            _cache = cache;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(AppleExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<AppleExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetBaseUrl();
            model.RedirectUrl = $"{host}signin-apple";

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.Apple.Auth").Metadata;

            // INFO: No invalidation needed as the cache will be cleared on app pool recycle anyway.
            model.DisplayIISUserProfileWarning = _shouldEnableIISUserProfile.Value;

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

        private static bool ShouldEnableIISUserProfile()
        {
            // Non-Windows: IIS flag does not exist.
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                var sid = WindowsIdentity.GetCurrent().User?.Value;
                if (string.IsNullOrWhiteSpace(sid))
                {
                    return false;
                }

                using var hive = Registry.Users.OpenSubKey(sid, writable: false);
                return hive == null;
            }
            catch
            {
                return false;
            }
        }
    }
}