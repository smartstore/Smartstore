using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Google.MerchantCenter.Models;
using Smartstore.Web.Components;

namespace Smartstore.Google.MerchantCenter.Components
{
    /// <summary>
    /// Component to render profile configuration of GMC feed.
    /// </summary>
    public class GmcConfigurationViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(object data)
        {
            var model = data as ProfileConfigurationModel;

            ViewBag.LanguageSeoCode = Services.WorkContext.WorkingLanguage.UniqueSeoCode.EmptyNull().ToLower();
            ViewBag.AvailableCategories = model.DefaultGoogleCategory.HasValue()
                ? new List<SelectListItem> { new SelectListItem { Text = model.DefaultGoogleCategory, Value = model.DefaultGoogleCategory, Selected = true } }
                : new List<SelectListItem>();

            return View(model);
        }
    }
}
