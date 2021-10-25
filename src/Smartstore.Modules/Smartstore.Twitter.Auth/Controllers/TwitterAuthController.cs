using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Twitter.Auth.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Twitter.Auth.Controllers
{
    [Area("Admin")]
    [Route("[area]/twitterauth/[action]/{id?}")]
    public class TwitterAuthController : AdminController
    {
        [HttpGet, LoadSetting]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public IActionResult Configure(TwitterExternalAuthSettings settings)
        {
            var model = MiniMapper.Map<TwitterExternalAuthSettings, ConfigurationModel>(settings);
            var host = Services.StoreContext.CurrentStore.GetHost(true).EnsureEndsWith("/");
            model.RedirectUrl = $"{host}signin-twitter";

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

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
