using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.PayPal.Models;
using Smartstore.PayPal.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.PayPal.Controllers
{
    [Area("Admin")]
    [Route("[area]/paypal/{action=index}/{id?}")]
    public class PayPalAdminController : ModuleController
    {
        [LoadSetting]
        public IActionResult Configure(PayPalSettings settings)
        {
            var model = MiniMapper.Map<PayPalSettings, ConfigurationModel>(settings);

            model.EnabledFundings = settings.EnabledFundings.SplitSafe(',').ToArray();
            model.DisabledFundings = settings.DisabledFundings.SplitSafe(',').ToArray();
            model.WebhookUrl = Url.Action(nameof(PayPalController.WebhookHandler), "PayPal", null, "https");

            return View(model);
        }

        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, PayPalSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            string.Join(',', model.EnabledFundings ?? Array.Empty<string>());
            string.Join(',', model.DisabledFundings ?? Array.Empty<string>());

            return RedirectToAction(nameof(Configure));
        }
    }
}
