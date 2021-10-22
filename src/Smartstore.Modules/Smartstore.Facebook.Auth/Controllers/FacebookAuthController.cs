using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Facebook.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Facebook.Auth.Controllers
{
    [Area("Admin")]
    [Route("[area]/facebook/auth/[action]/{id?}")]
    public class FacebookAuthController : AdminController
    {
        private readonly Lazy<IConfigureNamedOptions<FacebookOptions>> _facebookOptionsConfigurer;
        private readonly IOptions<FacebookOptions> _facebookOptions;
        private readonly IOptionsMonitorCache<FacebookOptions> _optionsCache;

        public FacebookAuthController(
            Lazy<IConfigureNamedOptions<FacebookOptions>> facebookOptionsConfigurer, 
            IOptions<FacebookOptions> facebookOptions, 
            IOptionsMonitorCache<FacebookOptions> optionsCache)
        {
            _facebookOptionsConfigurer = facebookOptionsConfigurer;
            _facebookOptions = facebookOptions;
            _optionsCache = optionsCache;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(FacebookExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<FacebookExternalAuthSettings, ConfigurationModel>(settings);

            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-facebook";

            return View(model);
        }

        [HttpPost, SaveSetting]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public async Task<IActionResult> Configure(ConfigurationModel model, FacebookExternalAuthSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();

            var updateOptions = ShouldUpdateFacebookOptions(model, settings);
            
            MiniMapper.Map(model, settings);

            if (updateOptions)
            {
                // Save settings now so new values can be applied in FacebookOptionsConfigurer.
                await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);
                _facebookOptionsConfigurer.Value.Configure(FacebookDefaults.AuthenticationScheme, _facebookOptions.Value);

                // Clear options cache and add current options again.
                _optionsCache.TryRemove(FacebookDefaults.AuthenticationScheme);
                _optionsCache.TryAdd(FacebookDefaults.AuthenticationScheme, _facebookOptions.Value);
            }

            return RedirectToAction(nameof(Configure));
        }

        private static bool ShouldUpdateFacebookOptions(ConfigurationModel model, FacebookExternalAuthSettings settings)
        {
            return model.ClientKeyIdentifier != settings.ClientKeyIdentifier || model.ClientSecret != settings.ClientSecret;
        }
    }
}
