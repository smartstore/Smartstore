using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Identity;

namespace Smartstore.Web.Controllers
{
    public class IdentityController : SmartController
    {
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;

        public IdentityController(
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
        }

        #region Login / Logout / Register

        [HttpGet]
        [AllowAnonymous]
        [LocalizedRoute("/login", Name = "Login")]
        public IActionResult Login(bool? checkoutAsGuest, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var model = new LoginModel
            {
                CustomerLoginType = _customerSettings.CustomerLoginType,
                CheckoutAsGuest = checkoutAsGuest.GetValueOrDefault(),
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnLoginPage
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [LocalizedRoute("/register", Name = "Register")]
        public IActionResult Register(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            
            var model = new RegisterModel
            {

            };

            return View(model);
        }

        [LocalizedRoute("/logout", Name = "Logout")]
        public async Task<IActionResult> Logout()
        {
            var workContext = Services.WorkContext;
            var db = Services.DbContext;

            if (workContext.CurrentImpersonator != null)
            {
                // Logout impersonated customer
                workContext.CurrentCustomer.GenericAttributes.ImpersonatedCustomerId = null;
                await db.SaveChangesAsync();
                
                // Redirect back to customer details page (admin area)
                return RedirectToAction("Edit", "Customer", new { id = workContext.CurrentCustomer.Id, area = "Admin" });

            }
            else
            {
                // Standard logout
                Services.ActivityLogger.LogActivity("PublicStore.Logout", T("ActivityLog.PublicStore.Logout"));
                await db.SaveChangesAsync();

                await _signInManager.SignOutAsync();
                return RedirectToRoute("Homepage");
            }
        }

        #endregion
    }
}