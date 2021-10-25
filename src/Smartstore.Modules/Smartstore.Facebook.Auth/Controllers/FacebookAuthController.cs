using Microsoft.AspNetCore.Mvc;
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
        // TODO: (mh) (core) Restore code for clearing options from cache after discused & tested with MC.

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
        public IActionResult Configure(ConfigurationModel model, FacebookExternalAuthSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }
    }
}
