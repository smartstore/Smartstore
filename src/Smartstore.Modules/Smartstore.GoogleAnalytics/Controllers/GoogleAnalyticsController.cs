using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.GoogleAnalytics.Models;
using Smartstore.GoogleAnalytics.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.GoogleAnalytics.Controllers
{
    [Area("Admin")]
    public class GoogleAnalyticsController : ModuleController
    {
        [LoadSetting]
        public IActionResult Configure(GoogleAnalyticsSettings settings)
        {
            var model = MiniMapper.Map<GoogleAnalyticsSettings, ConfigurationModel>(settings);

            model.WidgetZone = settings.WidgetZone;
            ViewBag.AvailableZones = new List<SelectListItem>
            {
                new SelectListItem { Text = "<head> HTML tag", Value = "head", Selected = settings.WidgetZone == "head" },
                new SelectListItem { Text = "Before <body> end HTML tag", Value = "end", Selected = settings.WidgetZone == "end" }
            };

            return View(model);
        }

        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, GoogleAnalyticsSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost, SaveSetting]
        [ActionName("Configure"), FormValueRequired("restore-scripts")] 
        public IActionResult RestoreScripts(GoogleAnalyticsSettings settings)
        {
            settings.TrackingScript = AnalyticsScriptUtility.GetTrackingScript();
            settings.EcommerceScript = AnalyticsScriptUtility.GetEcommerceScript();
            settings.EcommerceDetailScript = AnalyticsScriptUtility.GetEcommerceDetailScript();

            return RedirectToAction(nameof(Configure));
        }
    }
}
