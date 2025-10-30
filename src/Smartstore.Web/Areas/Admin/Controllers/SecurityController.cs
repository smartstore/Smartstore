using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models;
using Smartstore.Admin.Models.Security;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models;

namespace Smartstore.Admin.Controllers
{
    public class SecurityController : AdminController
    {
        // INFO: instead, throw new AccessDeniedException()

        //[ValidateAdminIpAddress(false)]
        //public IActionResult AccessDenied(string pageUrl)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;

        //    if (customer == null || customer.IsGuest())
        //    {
        //        Logger.Info(T("Admin.System.Warnings.AccessDeniedToAnonymousRequest", pageUrl.NaIfEmpty()));
        //        return View();
        //    }

        //    Logger.Info(T("Admin.System.Warnings.AccessDeniedToUser",
        //        customer.Email.NaIfEmpty(), 
        //        customer.Email.NaIfEmpty(), 
        //        pageUrl.NaIfEmpty()));

        //    return View();
        //}

        /// <summary>
        /// Called by AJAX
        /// </summary>
        public async Task<IActionResult> AllAccessPermissions(string selected)
        {
            var systemNames = await Services.Permissions.GetAllSystemNamesAsync();
            var selectedArr = selected.SplitSafe(',');

            var data = systemNames
                .Select(x => new ChoiceListItem
                {
                    Id = x.Key,
                    Text = x.Value,
                    Selected = selectedArr.Contains(x.Key)
                })
                .ToList();

            return Json(data);
        }

        [LoadSetting]
        public IActionResult GoogleRecaptcha(GoogleRecaptchaSettings settings, string btnId)
        {
            var model = MiniMapper.Map<GoogleRecaptchaSettings, GoogleRecaptchaModel>(settings);
            PrepareGoogleRecaptchaModel(model, btnId);
            return View(model);
        }

        [SaveSetting]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult GoogleRecaptcha(GoogleRecaptchaModel model, GoogleRecaptchaSettings settings, string btnId, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                ModelState.Clear();

                MiniMapper.Map(model, settings);

                ViewBag.RefreshPage = true;
                ViewBag.CloseWindow = !continueEditing;
            }

            PrepareGoogleRecaptchaModel(model, btnId);

            return View(model);
        }

        private void PrepareGoogleRecaptchaModel(GoogleRecaptchaModel model, string btnId)
        {
            ViewBag.BtnId = btnId;

            ViewBag.AvailableVersions = new SelectList(new List<SelectListItem>
            {
                new() { Value = "v2", Text = "v2" },
                new() { Value = "v3", Text = "v3" }
            }, "Value", "Text", model.Version);

            var resPrefix = "Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.Size.";
            ViewBag.AvailableSizes = new SelectList(new List<SelectListItem>
            {
                new() { Value = "normal", Text = T(resPrefix + "Normal") },
                new() { Value = "compact", Text = T(resPrefix + "Compact") },
                new() { Value = "invisible", Text = T(resPrefix + "Invisible") }
            }, "Value", "Text", model.Size);

            resPrefix = "Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.BadgePosition.";
            ViewBag.AvailableBadgePositions = new SelectList(new List<SelectListItem>
            {
                new() { Value = "bottomleft", Text = T(resPrefix + "BottomLeft") },
                new() { Value = "bottomright", Text = T(resPrefix + "BottomRight") },
                new() { Value = "inline", Text = T(resPrefix + "Inline") },
                new() { Value = "hide", Text = T(resPrefix + "Hide") },
            }, "Value", "Text", model.BadgePosition);
        }

        public IActionResult CheckCaptchaConfigured(string systemName)
        {
            var captchaManager = Services.Resolve<ICaptchaManager>();
            var configured = captchaManager.GetProviderBySystemName(systemName)?.Value?.IsConfigured == true;

            return Json(new { configured });
        }
    }
}
