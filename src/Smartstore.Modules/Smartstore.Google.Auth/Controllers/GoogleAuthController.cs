using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Google.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Google.Auth.Controllers
{
    [Area("Admin")]
    [Route("[area]/googleauth/[action]/{id?}")]
    public class GoogleAuthController : AdminController
    {
        private readonly IOptionsMonitorCache<GoogleOptions> _optionsCache;

        public GoogleAuthController(IOptionsMonitorCache<GoogleOptions> optionsCache)
        {
            _optionsCache = optionsCache;
        }

        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(GoogleExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<GoogleExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-google";

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
