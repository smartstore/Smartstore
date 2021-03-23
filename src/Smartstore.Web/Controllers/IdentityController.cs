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
        [AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/login", Name = "Login")]
        public IActionResult Login(bool? checkoutAsGuest, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Content("~/");

            var model = new LoginModel
            {
                CustomerLoginType = _customerSettings.CustomerLoginType,
                CheckoutAsGuest = checkoutAsGuest.GetValueOrDefault(),
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnLoginPage
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous, NeverAuthorize]
        [ValidateAntiForgeryToken, ValidateCaptcha]
        [LocalizedRoute("/login", Name = "Login")]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl, string captchaError)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                string userNameOrEmail;

                if (model.CustomerLoginType == CustomerLoginType.Username)
                {
                    userNameOrEmail = model.Username;
                }
                else if (model.CustomerLoginType == CustomerLoginType.Email)
                {
                    userNameOrEmail = model.Email;
                }
                else
                {
                    userNameOrEmail = model.UsernameOrEmail;
                }

                userNameOrEmail = userNameOrEmail.TrimSafe();

                var result = await _signInManager.PasswordSignInAsync(userNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (returnUrl.IsEmpty() || !Url.IsLocalUrl(returnUrl))
                    {
                        return RedirectToRoute("Login");
                    }

                    return RedirectToReferrer(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, T("Account.Login.WrongCredentials"));
                }
            }

            // If we got this far, something failed, redisplay form.
            model.CustomerLoginType = _customerSettings.CustomerLoginType;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnLoginPage;

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/register", Name = "Register")]
        public IActionResult Register(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            
            var model = new RegisterModel
            {
                UserNamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous, NeverAuthorize]
        [ValidateAntiForgeryToken, ValidateCaptcha, ValidateHoneypot]
        [LocalizedRoute("/register", Name = "Register")]
        public async Task<IActionResult> Register(RegisterModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = new Customer 
                { 
                    Username = model.UserName, 
                    Email = model.Email, 
                    PasswordFormat = _customerSettings.DefaultPasswordFormat,
                    Active = true,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastActivityDateUtc = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    "Please confirm your account by clicking this link: <a href=\"" + callbackUrl + "\">link</a>");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    Logger.Info("User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [NeverAuthorize]
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
                
                await _signInManager.SignOutAsync();
                await db.SaveChangesAsync();

                return RedirectToRoute("Login");
            }
        }

        #endregion

        #region Access

        [HttpGet]
        [AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/access-denied", Name = "AccessDenied")]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            return Content("TODO: Make AccessDenied view");
        }

        #endregion

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            return RedirectToReferrer(returnUrl, () => RedirectToRoute("Login"));
        }

        #endregion
    }
}