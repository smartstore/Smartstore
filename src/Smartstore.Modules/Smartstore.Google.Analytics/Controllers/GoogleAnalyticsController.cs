using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Google.Analytics.Models;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Google.Analytics.Controllers
{
    [Area("Admin")]
    public class GoogleAnalyticsController : ModuleController
    {
        [LoadSetting]
        public IActionResult Configure(GoogleAnalyticsSettings settings)
        {
            var model = MiniMapper.Map<GoogleAnalyticsSettings, ConfigurationModel>(settings);

            model.WidgetZone = settings.WidgetZone;
            PrepareConfigModel(settings.WidgetZone);

            return View(model);
        }

        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, GoogleAnalyticsSettings settings)
        {
            if (!ModelState.IsValid)
            {
                PrepareConfigModel(settings.WidgetZone);
                return View(model);
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

        private void PrepareConfigModel(string widgetZone)
        {
            ViewBag.AvailableZones = new List<SelectListItem>
            {
                new SelectListItem { Text = "<head> HTML tag", Value = "head", Selected = widgetZone == "head" },
                new SelectListItem { Text = "Before <body> end HTML tag", Value = "end", Selected = widgetZone == "end" }
            };
        }
    }
}
