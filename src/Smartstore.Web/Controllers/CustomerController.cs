using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public class CustomerController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly INewsletterSubscriptionService _newsletterSubscriptionService;
        private readonly ITaxService _taxService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IMessageFactory _messageFactory;
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly IProviderManager _providerManager;
        private readonly ICacheManager _cache;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly OrderSettings _orderSettings;

        public CustomerController(
            SmartDbContext db,
            INewsletterSubscriptionService newsletterSubscriptionService,
            ITaxService taxService,
            ILocalizationService localizationService,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IMessageFactory messageFactory,
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            IProviderManager providerManager,
            ICacheManager cache,
            IDateTimeHelper dateTimeHelper,
            DateTimeSettings dateTimeSettings,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _newsletterSubscriptionService = newsletterSubscriptionService;
            _taxService = taxService;
            _localizationService = localizationService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _messageFactory = messageFactory;
            _userManager = userManager;
            _signInManager = signInManager;
            _providerManager = providerManager;
            _cache = cache;
            _dateTimeHelper = dateTimeHelper;
            _dateTimeSettings = dateTimeSettings;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
            _localizationSettings = localizationSettings;
            _orderSettings = orderSettings;
        }

        [RequireSsl]
        public async Task<IActionResult> Info()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }
            
            var model = new CustomerInfoModel();
            await PrepareCustomerInfoModelAsync(model, customer, false);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Info(CustomerInfoModel model)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            if (model.Email.IsEmpty())
            {
                ModelState.AddModelError("", "Email is not provided.");
            }
            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && _customerSettings.AllowUsersToChangeUsernames && model.Username.IsEmpty())
            {
                ModelState.AddModelError("", "Username is not provided.");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    customer.FirstName = model.FirstName;
                    customer.LastName = model.LastName;

                    // Username.
                    if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && _customerSettings.AllowUsersToChangeUsernames)
                    {
                        if (!customer.Username.EqualsNoCase(model.Username.Trim()))
                        {
                            // Change username.
                            await _userManager.SetUserNameAsync(customer, model.Username.Trim());
                            // Re-authenticate.
                            await _signInManager.SignInAsync(customer, true);
                        }
                    }

                    // Email.
                    if (!customer.Email.Equals(model.Email.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Change email.
                        await _userManager.SetEmailAsync(customer, model.Email.Trim());
                        // Re-authenticate (if usernames are disabled).
                        if (_customerSettings.CustomerLoginType == CustomerLoginType.Email)
                        {
                            await _signInManager.SignInAsync(customer, true);
                        }
                    }

                    // VAT number.
                    if (_taxSettings.EuVatEnabled)
                    {
                        var prevVatNumber = customer.GenericAttributes.VatNumber;
                        customer.GenericAttributes.VatNumber = model.VatNumber;

                        if (prevVatNumber != model.VatNumber)
                        {
                            (var vatNumberStatus, var vatName, var vatAddress) = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                            customer.VatNumberStatusId = (int)vatNumberStatus;

                            // Send VAT number admin notification.
                            if (model.VatNumber.HasValue() && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                            {
                                await _messageFactory.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatAddress, _localizationSettings.DefaultAdminLanguageId);
                            }
                        }
                    }

                    // Customer number.
                    if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
                    {
                        var numberExists = await _db.Customers.Where(x => x.CustomerNumber == model.CustomerNumber).AnyAsync();

                        if (model.CustomerNumber != customer.CustomerNumber && numberExists)
                        {
                            NotifyError("Common.CustomerNumberAlreadyExists");
                        }
                        else
                        {
                            customer.CustomerNumber = model.CustomerNumber;
                        }
                    }

                    if (_customerSettings.DateOfBirthEnabled)
                    {
                        try
                        {
                            if (model.DateOfBirthYear.HasValue && model.DateOfBirthMonth.HasValue && model.DateOfBirthDay.HasValue)
                            {
                                customer.BirthDate = new DateTime(model.DateOfBirthYear.Value, model.DateOfBirthMonth.Value, model.DateOfBirthDay.Value);
                            }
                            else
                            {
                                customer.BirthDate = null;
                            }
                        }
                        catch { }
                    }

                    if (_customerSettings.CompanyEnabled)
                    {
                        customer.Company = model.Company;
                    }
                    if (_customerSettings.TitleEnabled)
                    {
                        customer.Title = model.Title;
                    }
                    if (_customerSettings.GenderEnabled)
                    {
                        customer.Gender = model.Gender;
                    }
                    if (_customerSettings.StreetAddressEnabled)
                    {
                        customer.GenericAttributes.StreetAddress = model.StreetAddress;
                    }
                    if (_customerSettings.StreetAddress2Enabled)
                    {
                        customer.GenericAttributes.StreetAddress2 = model.StreetAddress2;
                    }
                    if (_customerSettings.ZipPostalCodeEnabled)
                    {
                        customer.GenericAttributes.ZipPostalCode = model.ZipPostalCode;
                    }
                    if (_customerSettings.CityEnabled)
                    {
                        customer.GenericAttributes.City = model.City;
                    }
                    if (_customerSettings.CountryEnabled)
                    {
                        customer.GenericAttributes.CountryId = model.CountryId;
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
                    if (_customerSettings.NewsletterEnabled)
                    {
                        await _newsletterSubscriptionService.ApplySubscriptionAsync(model.Newsletter, customer.Email, Services.StoreContext.CurrentStore.Id);
                    }
                    // TODO: (mh) (core) Forum module stuff.
                    //if (_forumSettings.ForumsEnabled && _forumSettings.SignaturesEnabled)
                    //{
                    //    customer.GenericAttributes.Signature = model.Signature;
                    //}
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    {
                        customer.TimeZoneId = model.TimeZoneId;
                    }

                    await _db.SaveChangesAsync();
                    
                    return RedirectToAction("Info");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            await PrepareCustomerInfoModelAsync(model, customer, false);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            var usernameAvailable = false;
            var statusText = T("Account.CheckUsernameAvailability.NotAvailable");

            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && username != null)
            {
                username = username.Trim();

                if (username.HasValue())
                {
                    var customer = Services.WorkContext.CurrentCustomer;
                    if (customer != null && customer.Username != null && customer.Username.EqualsNoCase(username))
                    {
                        statusText = T("Account.CheckUsernameAvailability.CurrentUsername");
                    }
                    else
                    {
                        var userExists = await _db.Customers
                            .AsNoTracking()
                            .ApplyIdentFilter(userName: username)
                            .AnyAsync();

                        if (!userExists)
                        {
                            statusText = T("Account.CheckUsernameAvailability.Available");
                            usernameAvailable = true;
                        }
                    }
                }
            }

            return Json(new { Available = usernameAvailable, Text = statusText.Value });
        }

        #region Addresses

        [RequireSsl]
        public async Task<IActionResult> Addresses()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            var model = new List<AddressModel>();
            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id)
                .ToListAsync();

            foreach (var address in customer.Addresses)
            {
                var addressModel = new AddressModel();
                await MapperFactory.MapAsync(address, addressModel, new { excludeProperties = false, countries } );
                model.Add(addressModel);
            }

            return View(model);
        }

        [RequireSsl]
        public async Task<IActionResult> AddressDelete(int id)
        {
            if (id < 1)
                return NotFound();

            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
            if (address != null)
            {
                customer.RemoveAddress(address);
                // Now delete the address record.
                _db.Addresses.Remove(address);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Addresses");
        }

        [RequireSsl]
        public async Task<IActionResult> AddressAdd()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            var model = new AddressModel();
            await MapperFactory.MapAsync(new Address(), model);
            model.Email = customer?.Email;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddressAdd(AddressModel model)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            if (ModelState.IsValid)
            {
                var address = new Address();
                MiniMapper.Map(model, address);
                customer.Addresses.Add(address);

                await _db.SaveChangesAsync();

                return RedirectToAction("Addresses");
            }

            // If we got this far something failed. Redisplay form.
            await MapperFactory.MapAsync(new Address(), model);

            return View(model);
        }

        [RequireSsl]
        public async Task<IActionResult> AddressEdit(int id)
        {
            if (id < 1)
            {
                return NotFound();
            }
            
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
            if (address == null)
            {
                return RedirectToAction("Addresses");
            }
            
            var model = new AddressModel();
            await MapperFactory.MapAsync(address, model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddressEdit(AddressModel model, int id)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
            if (address == null)
            {
                return RedirectToAction("Addresses");
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, address);
                _db.Addresses.Update(address);
                await _db.SaveChangesAsync();

                return RedirectToAction("Addresses");
            }

            // If we got this far something failed. Redisplay form.
            await MapperFactory.MapAsync(new Address(), model);

            return View(model);
        }

        #endregion

        #region Orders

        [RequireSsl]
        public async Task<IActionResult> Orders(int? page, int? recurringPaymentsPage)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            var ordersPageIndex = Math.Max((page ?? 0) - 1, 0);
            var rpPageIndex = Math.Max((recurringPaymentsPage ?? 0) - 1, 0);

            var model = await PrepareCustomerOrderListModelAsync(customer, ordersPageIndex, rpPageIndex);
            model.OrdersPage = page;
            model.RecurringPaymentsPage = recurringPaymentsPage;

            return View(model);
        }

        // TODO: (mh) (core) Test this.
        [HttpPost, ActionName("Orders")]
        [FormValueRequired(FormValueRequirementOperator.StartsWith, "cancelRecurringPayment")]
        public async Task<IActionResult> CancelRecurringPayment(FormCollection form)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return new UnauthorizedResult();
            }

            // Get recurring payment identifier.
            var recurringPaymentId = 0;
            foreach (var formValue in form.Keys)
            {
                if (formValue.StartsWith("cancelRecurringPayment", StringComparison.InvariantCultureIgnoreCase))
                {
                    recurringPaymentId = Convert.ToInt32(formValue["cancelRecurringPayment".Length..]);
                }
            }

            var recurringPayment = await _db.RecurringPayments.FindByIdAsync(recurringPaymentId, false);
            if (recurringPayment == null)
            {
                return RedirectToAction("Orders");
            }

            if (recurringPayment.IsCancelable(customer))
            {
                var errors = await _orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);
                var model = await PrepareCustomerOrderListModelAsync(customer, 0, 0);
                model.CancelRecurringPaymentErrors = errors.ToList();

                return View(model);
            }

            return RedirectToAction("Orders");
        }

        #endregion

        [NonAction]
        protected async Task PrepareCustomerInfoModelAsync(CustomerInfoModel model, Customer customer, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(customer, nameof(customer));

            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;

            var availableTimeZones = new List<SelectListItem>();
            foreach (var tzi in _dateTimeHelper.GetSystemTimeZones())
            {
                availableTimeZones.Add(new SelectListItem
                {
                    Text = tzi.DisplayName,
                    Value = tzi.Id,
                    Selected = (excludeProperties ? tzi.Id == model.TimeZoneId : tzi.Id == _dateTimeHelper.CurrentTimeZone.Id)
                });
            }
            ViewBag.AvailableTimeZones = availableTimeZones;

            if (!excludeProperties)
            {
                var dateOfBirth = customer.BirthDate;

                var newsletterSubscription = await _db.NewsletterSubscriptions
                    .AsNoTracking()
                    .ApplyMailAddressFilter(customer.Email, Services.StoreContext.CurrentStore.Id)
                    .FirstOrDefaultAsync();

                model.Company = customer.Company;
                model.Title = customer.Title;
                model.FirstName = customer.FirstName;
                model.LastName = customer.LastName;
                model.Gender = customer.Gender;
                model.CustomerNumber = customer.CustomerNumber;
                model.Email = customer.Email;
                model.Username = customer.Username;

                if (dateOfBirth.HasValue)
                {
                    model.DateOfBirthDay = dateOfBirth.Value.Day;
                    model.DateOfBirthMonth = dateOfBirth.Value.Month;
                    model.DateOfBirthYear = dateOfBirth.Value.Year;
                }

                model.VatNumber = customer.GenericAttributes.VatNumber;
                model.StreetAddress = customer.GenericAttributes.StreetAddress;
                model.StreetAddress2 = customer.GenericAttributes.StreetAddress2;
                model.City = customer.GenericAttributes.City;
                model.ZipPostalCode = customer.GenericAttributes.ZipPostalCode;
                model.CountryId = (int)customer.GenericAttributes.CountryId;
                model.StateProvinceId = (int)customer.GenericAttributes.StateProvinceId;
                model.Phone = customer.GenericAttributes.Phone;
                model.Fax = customer.GenericAttributes.Fax;
                model.Signature = customer.GenericAttributes.Signature;
                model.Newsletter = newsletterSubscription != null && newsletterSubscription.Active;
            }
            else
            {
                if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && !_customerSettings.AllowUsersToChangeUsernames)
                {
                    model.Username = customer.Username;
                }
            }

            // TODO: (mh) (core) Make taghelpers or some other generic solution for this always repeating code.
            // Countries and states.
            if (_customerSettings.CountryEnabled)
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
                        Selected = c.Id == model.CountryId
                    });
                }

                ViewBag.AvailableCountries = availableCountries;

                if (_customerSettings.StateProvinceEnabled)
                {
                    var availableStates = new List<SelectListItem>();

                    var states = await _db.StateProvinces
                        .AsNoTracking()
                        .ApplyCountryFilter(model.CountryId)
                        .ToListAsync();

                    if (states.Any())
                    {
                        foreach (var s in states)
                        {
                            availableStates.Add(new SelectListItem
                            {
                                Text = s.GetLocalized(x => x.Name),
                                Value = s.Id.ToString(),
                                Selected = (s.Id == model.StateProvinceId)
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

            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            model.VatNumberStatusNote = await ((VatNumberStatus)customer.VatNumberStatusId).GetLocalizedEnumAsync(Services.WorkContext.WorkingLanguage.Id);
            model.GenderEnabled = _customerSettings.GenderEnabled;
            model.TitleEnabled = _customerSettings.TitleEnabled;
            model.DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled;
            model.CompanyEnabled = _customerSettings.CompanyEnabled;
            model.CompanyRequired = _customerSettings.CompanyRequired;
            model.StreetAddressEnabled = _customerSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _customerSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _customerSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _customerSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _customerSettings.ZipPostalCodeRequired;
            model.CityEnabled = _customerSettings.CityEnabled;
            model.CityRequired = _customerSettings.CityRequired;
            model.CountryEnabled = _customerSettings.CountryEnabled;
            model.StateProvinceEnabled = _customerSettings.StateProvinceEnabled;
            model.PhoneEnabled = _customerSettings.PhoneEnabled;
            model.PhoneRequired = _customerSettings.PhoneRequired;
            model.FaxEnabled = _customerSettings.FaxEnabled;
            model.FaxRequired = _customerSettings.FaxRequired;
            model.NewsletterEnabled = _customerSettings.NewsletterEnabled;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            model.AllowUsersToChangeUsernames = _customerSettings.AllowUsersToChangeUsernames;
            model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
            // TODO: (mh) (core) This must be injected by Forum module somehow.
            //model.SignatureEnabled = _forumSettings.ForumsEnabled && _forumSettings.SignaturesEnabled;
            model.DisplayCustomerNumber = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
                && _customerSettings.CustomerNumberVisibility != CustomerNumberVisibility.None;

            if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
                && (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.Editable
                || (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.EditableIfEmpty && model.CustomerNumber.IsEmpty())))
            {
                model.CustomerNumberEnabled = true;
            }
            else
            {
                model.CustomerNumberEnabled = false;
            }

            // TODO: (mh) (core) Implement when IExternalAuthenticationMethod and stuff is available.
            // External authentication.
            foreach (var ear in customer.ExternalAuthenticationRecords)
            {
                //var authMethod = _providerManager.GetProvider<IExternalAuthenticationMethod>(systemName, storeId);
                //var authMethod = _openAuthenticationService.LoadExternalAuthenticationMethodBySystemName(ear.ProviderSystemName);
                //if (authMethod == null || !authMethod.IsMethodActive(_externalAuthenticationSettings))
                //  continue;
                //    model.AssociatedExternalAuthRecords.Add(new CustomerInfoModel.AssociatedExternalAuthModel
                //    {
                //        Id = ear.Id,
                //        Email = ear.Email,
                //        ExternalIdentifier = ear.ExternalIdentifier,
                //        AuthMethodName = _pluginMediator.GetLocalizedFriendlyName(authMethod.Metadata, _workContext.WorkingLanguage.Id)
                //    });
            }
        }

        [NonAction]
        protected async Task<CustomerOrderListModel> PrepareCustomerOrderListModelAsync(Customer customer, int orderPageIndex, int recurringPaymentPageIndex)
        {
            Guard.NotNull(customer, nameof(customer));

            var store = Services.StoreContext.CurrentStore;
            var model = new CustomerOrderListModel();

            var orders = await _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(customer.Id, _orderSettings.DisplayOrdersOfAllStores ? 0 : store.Id)
                .ToPagedList(orderPageIndex, _orderSettings.OrderListPageSize)
                .LoadAsync();

            var orderModels = await orders
                .SelectAsync(async x =>
                {
                    var orderModel = new CustomerOrderListModel.OrderDetailsModel
                    {
                        Id = x.Id,
                        OrderNumber = x.GetOrderNumber(),
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                        OrderStatus = await _localizationService.GetLocalizedEnumAsync(x.OrderStatus),
                        IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(x)
                    };

                    // TODO: (mh) (core) Check format!
                    (var orderTotal, var roundingAmount) = await _orderService.GetOrderTotalInCustomerCurrencyAsync(x);
                    orderModel.OrderTotal = orderTotal;

                    return orderModel;
                })
                .AsyncToList();

            model.Orders = orderModels.ToPagedList(orders.PageIndex, orders.PageSize, orders.TotalCount);

            // Recurring payments.
            var recurringPayments = await _db.RecurringPayments
                .AsNoTracking()
                .ApplyStandardFilter(customerId: customer.Id, storeId: store.Id)
                .ToPagedList(recurringPaymentPageIndex, _orderSettings.RecurringPaymentListPageSize)
                .LoadAsync();

            var rpModels = await recurringPayments
                .SelectAsync(async x =>
                {
                    return new CustomerOrderListModel.RecurringPaymentModel
                    {
                        Id = x.Id,
                        StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc, DateTimeKind.Utc).ToString(),
                        CycleInfo = $"{x.CycleLength} {await _localizationService.GetLocalizedEnumAsync(x.CyclePeriod)}",
                        NextPayment = x.NextPaymentDate.HasValue ? _dateTimeHelper.ConvertToUserTime(x.NextPaymentDate.Value, DateTimeKind.Utc).ToString() : string.Empty,
                        TotalCycles = x.TotalCycles,
                        CyclesRemaining = x.CyclesRemaining,
                        InitialOrderId = x.InitialOrder.Id,
                        CanCancel = x.IsCancelable(customer)
                    };
                })
                .AsyncToList();

            model.RecurringPayments = rpModels.ToPagedList(recurringPayments.PageIndex, recurringPayments.PageSize, recurringPayments.TotalCount);

            return model;
        }

        // INFO: (mh) (core) Current CountryController just has this one method. Details TBD.
        // RE: But it does NOT belong here. Find another - perhaps more generic - controller please.
        /// <summary>
        /// This action method gets called via an ajax request.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> StatesByCountryId(string countryId, bool addEmptyStateIfRequired)
        {
            // TODO: (mh) (core) Don't throw in frontend.
            if (!countryId.HasValue())
                throw new ArgumentNullException(nameof(countryId));

            string cacheKey = string.Format(ModelCacheInvalidator.STATEPROVINCES_BY_COUNTRY_MODEL_KEY, countryId, addEmptyStateIfRequired, Services.WorkContext.WorkingLanguage.Id);
            var cacheModel = await _cache.GetAsync(cacheKey, async () =>
            {
                var country = await _db.Countries
                    .AsNoTracking()
                    .Where(x => x.Id == Convert.ToInt32(countryId))
                    .FirstOrDefaultAsync();

                var states = await _db.StateProvinces
                    .AsNoTracking()
                    .ApplyCountryFilter(country != null ? country.Id : 0)
                    .ToListAsync();

                var result = (from s in states
                              select new { id = s.Id, name = s.GetLocalized(x => x.Name).Value })
                              .ToList();

                if (addEmptyStateIfRequired && result.Count == 0)
                {
                    result.Insert(0, new { id = 0, name = T("Address.OtherNonUS").Value });
                }
                
                return result;
            });

            return Json(cacheModel);
        }
    }
}
