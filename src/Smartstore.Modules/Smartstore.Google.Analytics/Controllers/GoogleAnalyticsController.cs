using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Google.Analytics.Models;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Google.Analytics.Controllers
{
    [Area("Admin")]
    public class GoogleAnalyticsController : ModuleController
    {
        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(GoogleAnalyticsSettings settings)
        {
            var model = MiniMapper.Map<GoogleAnalyticsSettings, ConfigurationModel>(settings);

            model.WidgetZone = settings.WidgetZone;
            PrepareConfigModel(settings.WidgetZone);

            // If old script is used display hint to click on the 'Restore Scripts' button to get up-to-date scrpt snippets.
            model.ScriptUpdateRecommended = settings.EcommerceScript.Contains("analytics.js");

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
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

        [HttpPost, SaveSetting, AuthorizeAdmin]
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
