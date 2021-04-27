using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Web.Models.Identity;

namespace Smartstore.Web.Controllers
{
    public class IdentityController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly RoleManager<CustomerRole> _roleManager;
        private readonly ITaxService _taxService;
        private readonly IAddressService _addressService;
        private readonly IMessageFactory _messageFactory;
        private readonly IWebHelper _webHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly TaxSettings _taxSettings;
        private readonly LocalizationSettings _localizationSettings;
        
        public IdentityController(
            SmartDbContext db,
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            RoleManager<CustomerRole> roleManager,
            ITaxService taxService,
            IAddressService addressService,
            IMessageFactory messageFactory,
            IWebHelper webHelper,
            IDateTimeHelper dateTimeHelper,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            DateTimeSettings dateTimeSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _taxService = taxService;
            _addressService = addressService;
            _messageFactory = messageFactory;
            _webHelper = webHelper;
            _dateTimeHelper = dateTimeHelper;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
            _dateTimeSettings = dateTimeSettings;
            _taxSettings = taxSettings;
            _localizationSettings = localizationSettings;
        }

        #region Login / Logout / Register

        [HttpGet]
        [RequireSsl, AllowAnonymous, NeverAuthorize]
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
                        return RedirectToRoute("Homepage");
                    }

                    return RedirectToReferrer(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, T("Account.Login.WrongCredentials"));
                }
            }

            // If we got this far something failed. Redisplay form!
            model.CustomerLoginType = _customerSettings.CustomerLoginType;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnLoginPage;

            return View(model);
        }

        [HttpGet]
        [RequireSsl, AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/register", Name = "Register")]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
            // Check whether registration is allowed.
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            {
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });
            }

            ViewBag.ReturnUrl = returnUrl;

            var model = new RegisterModel();
            await PrepareRegisterModelAsync(model);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous, NeverAuthorize]
        [ValidateAntiForgeryToken, ValidateCaptcha, ValidateHoneypot]
        [LocalizedRoute("/register", Name = "Register")]
        public async Task<IActionResult> Register(RegisterModel model, string returnUrl = null)
        {
            // Check whether registration is allowed.
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            {
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });
            }

            var customer = Services.WorkContext.CurrentCustomer;
            if (customer.IsRegistered())
            {
                // Already registered customer. 
                await _signInManager.SignOutAsync();

                Services.WorkContext.CurrentCustomer = null;
            }

            // TODO: (mh) (core) AddCaptcha error to model state? RE: Yes, absolutely!

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Lets remove spaces from login data.
                model.UserName = model.UserName.Trim();
                model.Email = model.Email.Trim();

                bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;

                customer = new Customer 
                { 
                    Username = model.UserName, 
                    Email = model.Email, 
                    PasswordFormat = _customerSettings.DefaultPasswordFormat,
                    Active = isApproved,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastActivityDateUtc = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(customer, model.Password);

                if (result.Succeeded)
                {
                    // TODO: (mh) (core) Bad API design (naming). Find a better name for this important "MAPPING" stuff.
                    await UpdateCustomerAsync(customer, model, isApproved);

                    await AssignCustomerRolesAsync(customer);

                    // TODO: (mh) (core) AddRewardPoints

                    await Services.EventPublisher.PublishAsync(new CustomerRegisteredEvent { Customer = customer });

                    // Notifications
                    if (_customerSettings.NotifyNewCustomerRegistration)
                    {
                        await _messageFactory.SendCustomerRegisteredNotificationMessageAsync(customer, _localizationSettings.DefaultAdminLanguageId);
                    }

                    switch (_customerSettings.UserRegistrationType)
                    {
                        case UserRegistrationType.EmailValidation:
                        {
                            // TODO: (mh) (core) Test!
                            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713

                            // Send an email with generated token.
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(customer);

                            // TODO: (mh) (core) The next can surely be done with _userManager. Find out how!
                            customer.GenericAttributes.AccountActivationToken = code;
                            await _db.SaveChangesAsync();
                            await _messageFactory.SendCustomerEmailValidationMessageAsync(customer, Services.WorkContext.WorkingLanguage.Id);

                            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });
                        }
                        case UserRegistrationType.AdminApproval:
                        {
                            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                        }
                        case UserRegistrationType.Standard:
                        {
                            // Send customer welcome message.
                            await _messageFactory.SendCustomerWelcomeMessageAsync(customer, Services.WorkContext.WorkingLanguage.Id);
                            await _signInManager.SignInAsync(customer, isPersistent: false);

                            var redirectUrl = Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
                            if (returnUrl.HasValue())
                            {
                                redirectUrl = _webHelper.ModifyQueryString(redirectUrl, "returnUrl=" + HttpUtility.UrlEncode(returnUrl), null);
                            }
                            
                            return Redirect(redirectUrl);
                        }
                        default:
                        {
                            return RedirectToRoute("Homepage");
                        }
                    }
                }

                AddErrors(result);
            }

            // If we got this far something failed. Redisplay form.
            await PrepareRegisterModelAsync(model);
            return View(model);
        }

        [HttpGet]
        [RequireSsl, AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/registerresult/{resultId:int}", Name = "RegisterResult")]
        public IActionResult RegisterResult(int resultId)
        {
            var resultText = string.Empty;
            switch ((UserRegistrationType)resultId)
            {
                case UserRegistrationType.Disabled:
                    // TODO: (mh) (core) This resource must be changed. But how to do it right now?
                    resultText = T("Account.Register.Result.Disabled");
                    break;
                case UserRegistrationType.Standard:
                    resultText = T("Account.Register.Result.Standard");
                    break;
                case UserRegistrationType.AdminApproval:
                    resultText = T("Account.Register.Result.AdminApproval");
                    break;
                case UserRegistrationType.EmailValidation:
                    resultText = T("Account.Register.Result.EmailValidation");
                    break;
                default:
                    break;
            }

            ViewBag.RegisterResult = resultText;
            return View();
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

        // TODO: (mh) (core) Change to /identity/activation ?
        [HttpGet]
        [RequireSsl, AllowAnonymous, NeverAuthorize]
        [LocalizedRoute("/customer/activation", Name = "AccountActivation")]
        public async Task<IActionResult> AccountActivation(string token, string email)
        {
            var customer = await _userManager.FindByEmailAsync(email);
                
            if (customer == null)
            {
                NotifyError(T("Account.AccountActivation.InvalidEmailOrToken"));
                return RedirectToRoute("Homepage");
            }

            var cToken = customer.GenericAttributes.AccountActivationToken;
            if (cToken.IsEmpty() || !cToken.Equals(token, StringComparison.InvariantCultureIgnoreCase))
            {
                NotifyError(T("Account.AccountActivation.InvalidEmailOrToken"));
                return RedirectToRoute("HomePage");
            }

            // Activate user account.
            customer.Active = true;

            // TODO: (mh) (core) Reset can probably be handled via user manager.
            customer.GenericAttributes.AccountActivationToken = string.Empty;
            await _db.SaveChangesAsync();

            // Send welcome message.
            await _messageFactory.SendCustomerWelcomeMessageAsync(customer, Services.WorkContext.WorkingLanguage.Id);

            ViewBag.ActivationResult = T("Account.AccountActivation.Activated");

            return View();
        }

        #endregion

        #region Change password

        [RequireSsl]
        public IActionResult ChangePassword()
        {
            if (!Services.WorkContext.CurrentCustomer.IsRegistered())
                return new UnauthorizedResult();

            var model = new ChangePasswordModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
                return new UnauthorizedResult();

            if (ModelState.IsValid)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(customer, model.OldPassword, model.NewPassword);
                
                if (changePasswordResult.Succeeded)
                {
                    model.Result = T("Account.ChangePassword.Success");
                    return View(model);
                }
                else
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(model);
        }

        #endregion

        #region Password recovery

        [RequireSsl]
        [LocalizedRoute("/passwordrecovery", Name = "PasswordRecovery")]
        public IActionResult PasswordRecovery()
        {
            var model = new PasswordRecoveryModel();
            return View(model);
        }

        [HttpPost]
        [LocalizedRoute("/passwordrecovery", Name = "PasswordRecovery")]
        [FormValueRequired("send-email")]
        public async Task<IActionResult> PasswordRecovery(PasswordRecoveryModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = await _userManager.FindByEmailAsync(model.Email);

                if (customer != null && customer.Active)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(customer);
                    
                    // TODO: (mh) (core) Why isn't this handled by UserStore?
                    customer.GenericAttributes.PasswordRecoveryToken = token;
                    await _db.SaveChangesAsync();

                    await _messageFactory.SendCustomerPasswordRecoveryMessageAsync(customer, Services.WorkContext.WorkingLanguage.Id);

                    model.ResultMessage = T("Account.PasswordRecovery.EmailHasBeenSent");
                    model.ResultState = PasswordRecoveryResultState.Success;
                }
                else
                {
                    model.ResultMessage = T("Account.PasswordRecovery.EmailNotFound");
                    model.ResultState = PasswordRecoveryResultState.Error;
                }

                return View(model);
            }

            // If we got this far something failed. Redisplay form.
            return View(model);
        }

        [RequireSsl]
        public IActionResult PasswordRecoveryConfirm(string token, string email)
        {
            // INFO: (mh) (core) Lets validate in POST.
            //var customer = await _db.Customers.Where(x => x.Email == email).FirstOrDefaultAsync();
            //var customerToken = customer.GenericAttributes.PasswordRecoveryToken;
            //if (customer == null || customerToken.IsEmpty() || customerToken != token)
            //{
            //    NotifyError(T("Account.PasswordRecoveryConfirm.InvalidEmailOrToken"));
            //}

            var model = new PasswordRecoveryConfirmModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost, ActionName("PasswordRecoveryConfirm")]
        [FormValueRequired("set-password")]
        public async Task<ActionResult> PasswordRecoveryConfirmPOST(PasswordRecoveryConfirmModel model)
        {
            var customer = await _userManager.FindByEmailAsync(model.Email);
            var customerToken = customer.GenericAttributes.PasswordRecoveryToken;
            if (customer == null || customerToken.IsEmpty() || customerToken != model.Token)
            {
                NotifyError(T("Account.PasswordRecoveryConfirm.InvalidEmailOrToken"));
                return RedirectToAction("PasswordRecoveryConfirm", new { token = model.Token, email = model.Email });
            }

            if (ModelState.IsValid)
            {
                var response = await _userManager.ResetPasswordAsync(customer, model.Token, model.NewPassword);
                
                if (response.Succeeded)
                {
                    customer.GenericAttributes.PasswordRecoveryToken = string.Empty;
                    await _db.SaveChangesAsync();
                    model.SuccessfullyChanged = true;
                    model.Result = T("Account.PasswordRecovery.PasswordHasBeenChanged");
                }
                else
                {
                    // TODO: (mh) (core) Change this to display all errors. 
                    model.Result = response.Errors.FirstOrDefault().Description;
                }

                return View(model);
            }

            // If we got this far something failed. Redisplay form.
            return View(model);
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

        private async Task PrepareRegisterModelAsync(RegisterModel model)
        {
            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;
            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            model.VatRequired = _taxSettings.VatRequired;
        
            MiniMapper.Map(_customerSettings, model);

            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnRegistrationPage;

            ViewBag.AvailableTimeZones = new List<SelectListItem>();
            foreach (var tzi in _dateTimeHelper.GetSystemTimeZones())
            {
                ViewBag.AvailableTimeZones.Add(new SelectListItem { Text = tzi.DisplayName, Value = tzi.Id, Selected = (tzi.Id == _dateTimeHelper.DefaultStoreTimeZone.Id) });
            }

            if (_customerSettings.CountryEnabled)
            {
                await AddCountriesAndStatesToViewBagAsync(model.CountryId, _customerSettings.StateProvinceEnabled, model.StateProvinceId ?? 0);
            }
        }

        private async Task UpdateCustomerAsync(Customer customer, RegisterModel model, bool isApproved)
        {
            // Properties
            if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                customer.TimeZoneId = model.TimeZoneId;

            // VAT number
            if (_taxSettings.EuVatEnabled)
            {
                customer.GenericAttributes.VatNumber = model.VatNumber;
                (var vatNumberStatus, _, var vatAddress) = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                customer.VatNumberStatusId = (int)vatNumberStatus;

                // Send VAT number admin notification.
                if (model.VatNumber.HasValue() && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                {
                    await _messageFactory.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatAddress, _localizationSettings.DefaultAdminLanguageId);
                }
            }

            // Form fields
            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;

            if (_customerSettings.CompanyEnabled)
            {
                customer.Company = model.Company;
            }
            
            if (_customerSettings.DateOfBirthEnabled)
            {
                try
                {
                    customer.BirthDate = new DateTime(model.DateOfBirthYear.Value, model.DateOfBirthMonth.Value, model.DateOfBirthDay.Value);
                }
                catch 
                { 
                }
            }

            if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.AutomaticallySet && customer.CustomerNumber.IsEmpty())
            {
                customer.CustomerNumber = customer.Id.Convert<string>();
            }
            if (_customerSettings.GenderEnabled)
            {
                customer.Gender = model.Gender;
            }
            if (_customerSettings.ZipPostalCodeEnabled)
            {
                customer.GenericAttributes.ZipPostalCode = model.ZipPostalCode;
            }
            if (_customerSettings.CountryEnabled)
            {
                customer.GenericAttributes.CountryId = model.CountryId;
            }
            if (_customerSettings.StreetAddressEnabled)
            {
                customer.GenericAttributes.StreetAddress = model.StreetAddress;
            }
            if (_customerSettings.StreetAddress2Enabled)
            {
                customer.GenericAttributes.StreetAddress2 = model.StreetAddress2;
            }
            if (_customerSettings.CityEnabled)
            {
                customer.GenericAttributes.City = model.City;
            }
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            {
                customer.GenericAttributes.StateProvinceId = model.StateProvinceId;
            }
            if (_customerSettings.PhoneEnabled)
            {
                customer.GenericAttributes.Phone = model.Phone;
            }
            if (_customerSettings.FaxEnabled)
            {
                customer.GenericAttributes.Fax = model.Fax;
            }
            
            // Newsletter subscription
            if (_customerSettings.NewsletterEnabled && model.Newsletter)
            {
                var subscription = await _db.NewsletterSubscriptions
                    .ApplyMailAddressFilter(model.Email, Services.StoreContext.CurrentStore.Id)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    subscription.Active = true;   
                }
                else
                {
                    subscription = new NewsletterSubscription
                    {
                        NewsletterSubscriptionGuid = Guid.NewGuid(),
                        Email = model.Email,
                        Active = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        StoreId = Services.StoreContext.CurrentStore.Id,
                        WorkingLanguageId = Services.WorkContext.WorkingLanguage.Id
                    };

                    _db.NewsletterSubscriptions.Add(subscription);
                }

                await _db.SaveChangesAsync();
            }

            // Login customer now.
            if (isApproved)
            {
                await _signInManager.SignInAsync(customer, true);
            }
            
            // TODO: (mh) (core) Take a closer look & implement!
            // Associated with external account (if possible)
            //TryAssociateAccountWithExternalAccount(customer);

            // Insert default address (if possible).
            var defaultAddress = new Address
            {
                Title = customer.Title,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Company = customer.Company,
                CountryId = customer.GenericAttributes.CountryId,
                ZipPostalCode = customer.GenericAttributes.ZipPostalCode,
                StateProvinceId = customer.GenericAttributes.StateProvinceId,
                City = customer.GenericAttributes.City,
                Address1 = customer.GenericAttributes.StreetAddress,
                Address2 = customer.GenericAttributes.StreetAddress2,
                PhoneNumber = customer.GenericAttributes.Phone,
                FaxNumber = customer.GenericAttributes.Fax,
                CreatedOnUtc = customer.CreatedOnUtc
            };

            if (await _addressService.IsAddressValidAsync(defaultAddress))
            {
                // Set default addresses.
                customer.Addresses.Add(defaultAddress);
                customer.BillingAddress = defaultAddress;
                customer.ShippingAddress = defaultAddress;
            }

            _db.TryUpdate(customer);
            await _db.SaveChangesAsync();
        }

        private async Task AssignCustomerRolesAsync(Customer customer)
        {
            // Add customer to 'Registered' role.
            var registeredRole = await _db.CustomerRoles
                .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
                .FirstOrDefaultAsync();

            await _userManager.AddToRoleAsync(customer, registeredRole.Name);

            // Add customer to custom configured role.
            if (_customerSettings.RegisterCustomerRoleId != 0 && _customerSettings.RegisterCustomerRoleId != registeredRole.Id)
            {
                // TODO: (mh) (core) Use extension method to pass id without casting, once available.
                var customerRole = await _roleManager.FindByIdAsync(_customerSettings.RegisterCustomerRoleId.ToString());
                if (customerRole != null)
                {
                    await _userManager.AddToRoleAsync(customer, customerRole.Name);
                }
            }

            // Remove customer from 'Guests' role.
            var mappings = customer
                .CustomerRoleMappings
                .Where(x => !x.IsSystemMapping && x.CustomerRole.SystemName == SystemCustomerRoleNames.Guests)
                .Select(x => x.CustomerRole.Name)
                .ToList();

            await _userManager.RemoveFromRolesAsync(customer, mappings);
            await _db.SaveChangesAsync();
        }

        //private async Task AssignCustomerRolesAsync(Customer customer)
        //{
        //    // Add customer to 'Registered' role.
        //    var registeredRole = await _db.CustomerRoles
        //        .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
        //        .FirstOrDefaultAsync();

        //    await _db.CustomerRoleMappings.AddAsync(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = registeredRole.Id });

        //    // Add customer to custom configured role.
        //    if (_customerSettings.RegisterCustomerRoleId != 0 && _customerSettings.RegisterCustomerRoleId != registeredRole.Id)
        //    {
        //        var customerRole = await _db.CustomerRoles.FindByIdAsync(_customerSettings.RegisterCustomerRoleId, false);
        //        if (customerRole != null && customerRole.Id != registeredRole.Id)
        //        {
        //            await _db.CustomerRoleMappings.AddAsync(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = customerRole.Id });
        //        }    
        //    }

        //    // Remove customer from 'Guests' role.
        //    var mappings = customer.CustomerRoleMappings.Where(x => !x.IsSystemMapping && x.CustomerRole.SystemName == SystemCustomerRoleNames.Guests).ToList();
        //    _db.CustomerRoleMappings.RemoveRange(mappings);
        //    await _db.SaveChangesAsync();
        //}

        // TODO: (mh) (core) Find globally accessable place for this.
        private async Task AddCountriesAndStatesToViewBagAsync(int selectedCountryId, bool statesEnabled, int selectedStateId)
        {
            var availableCountries = new List<SelectListItem>
            {
                new SelectListItem { Text = T("Address.SelectCountry"), Value = "0" }
            };

            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

            foreach (var c in countries)
            {
                availableCountries.Add(new SelectListItem
                {
                    Text = c.GetLocalized(x => x.Name),
                    Value = c.Id.ToString(),
                    Selected = c.Id == selectedCountryId
                });
            }

            ViewBag.AvailableCountries = availableCountries;

            if (statesEnabled)
            {
                var availableStates = new List<SelectListItem>();

                var states = await _db.StateProvinces
                    .AsNoTracking()
                    .ApplyCountryFilter(selectedStateId)
                    .ToListAsync();

                if (states.Any())
                {
                    foreach (var s in states)
                    {
                        availableStates.Add(new SelectListItem
                        {
                            Text = s.GetLocalized(x => x.Name),
                            Value = s.Id.ToString(),
                            Selected = s.Id == selectedStateId
                        });
                    }
                }
                else
                {
                    availableStates.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
                }

                ViewBag.AvailableStates = availableStates;
            }
        }

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