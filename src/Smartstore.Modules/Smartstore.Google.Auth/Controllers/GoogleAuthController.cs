using Microsoft.AspNetCore.Mvc;
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

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
