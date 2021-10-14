using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Customers;
using Smartstore.Admin.Models.ShoppingCart;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Net.Mail;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class CustomerController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICustomerService _customerService;
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly RoleManager<CustomerRole> _roleManager;
        private readonly IMessageFactory _messageFactory;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly Lazy<IEmailAccountService> _emailAccountService;
        private readonly Lazy<IGdprTool> _gdprTool;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly CustomerHelper _customerHelper;

        public CustomerController(
            SmartDbContext db,
            ICustomerService customerService,
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            RoleManager<CustomerRole> roleManager,
            IMessageFactory messageFactory,
            DateTimeSettings dateTimeSettings,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            CustomerSettings customerSettings,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            Lazy<IEmailAccountService> emailAccountService,
            Lazy<IGdprTool> gdprTool,
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IDateTimeHelper dateTimeHelper,
            CustomerHelper customerHelper)
        {
            _db = db;
            _customerService = customerService;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _messageFactory = messageFactory;
            _dateTimeSettings = dateTimeSettings;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _customerSettings = customerSettings;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _emailAccountService = emailAccountService;
            _gdprTool = gdprTool;
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _dateTimeHelper = dateTimeHelper;
            _customerHelper = customerHelper;
        }

        #region Utilities

        [NonAction]
        protected string GetCustomerRoleNames(IList<CustomerRole> customerRoles, string separator = ", ")
            => string.Join(separator, customerRoles.Select(x => x.Name));

        [NonAction]
        protected async Task<List<CustomerModel.AssociatedExternalAuthModel>> GetAssociatedExternalAuthRecordsAsync(Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));
            
            var result = new List<CustomerModel.AssociatedExternalAuthModel>();
            var authProviders = await _signInManager.GetExternalAuthenticationSchemesAsync();
            foreach (var ear in customer.ExternalAuthenticationRecords)
            {
                var provider = authProviders.Where(x => ear.ProviderSystemName.Contains(x.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (provider == null)
                    continue;

                result.Add(new CustomerModel.AssociatedExternalAuthModel
                {
                    Id = ear.Id,
                    Email = ear.Email,
                    ExternalIdentifier = ear.ExternalIdentifier,
                    AuthMethodName = provider.Name
                });
            }

            return result;
        }

        [NonAction]
        protected CustomerModel PrepareCustomerModelForList(Customer customer)
        {
            return new CustomerModel
            {
                Id = customer.Id,
                Email = customer.Email.HasValue() ? customer.Email : (customer.IsGuest() ? T("Admin.Customers.Guest").Value : string.Empty.NaIfEmpty()),
                Username = customer.Username,
                FullName = customer.GetFullName(),
                Company = customer.Company,
                CustomerNumber = customer.CustomerNumber,
                ZipPostalCode = customer.GenericAttributes.ZipPostalCode,
                Active = customer.Active,
                Phone = customer.GenericAttributes.Phone,
                CustomerRoleNames = GetCustomerRoleNames(customer.CustomerRoleMappings.Select(x => x.CustomerRole).ToList()),
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
                LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc),
                EditUrl = Url.Action("Edit", "Customer", new { id = customer.Id })
            };
        }

        protected virtual async Task PrepareCustomerModelForCreateAsync(CustomerModel model)
        {
            string timeZoneId = model.TimeZoneId.HasValue() ? model.TimeZoneId : Services.DateTimeHelper.DefaultStoreTimeZone.Id;

            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;
            model.DisplayVatNumber = false;

            if (model.SelectedCustomerRoleIds == null || model.SelectedCustomerRoleIds.Length == 0)
            {
                var role = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
                model.SelectedCustomerRoleIds = new int[] { role.Id };
            }

            model.AllowManagingCustomerRoles = await Services.Permissions.AuthorizeAsync(Permissions.Customer.EditRole);

            MiniMapper.Map(_customerSettings, model);

            model.CustomerNumberEnabled = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;

            await PrepareCountriesAndStatesAsync(model);

            ViewBag.AvailableTimeZones = new List<SelectListItem>();
            foreach (var tzi in Services.DateTimeHelper.GetSystemTimeZones())
            {
                ViewBag.AvailableTimeZones.Add(new SelectListItem { Text = tzi.DisplayName, Value = tzi.Id, Selected = tzi.Id == timeZoneId });
            }
        }

        protected virtual async Task PrepareCountriesAndStatesAsync(CustomerModel model)
        {
            // TODO: (mh) (core) Create some generic solution for this always repeating code.
            // RE: we made one already, didn't we?
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
        }

        protected virtual async Task PrepareCustomerModelForEditAsync(CustomerModel model, Customer customer)
        {
            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;
            model.Deleted = customer.Deleted;
            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            model.VatNumberStatusNote = await ((VatNumberStatus)customer.VatNumberStatusId).GetLocalizedEnumAsync();
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc);
            model.LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc);
            model.LastIpAddress = model.LastIpAddress;
            model.LastVisitedPage = customer.GenericAttributes.LastVisitedPage;

            // Form fields.
            MiniMapper.Map(_customerSettings, model);

            model.CustomerNumberEnabled = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            model.AllowManagingCustomerRoles = Services.Permissions.Authorize(Permissions.Customer.EditRole);
            model.DisplayRewardPointsHistory = _rewardPointsSettings.Enabled;
            model.AssociatedExternalAuthRecords = await GetAssociatedExternalAuthRecordsAsync(customer);
            model.PermissionTree = await Services.Permissions.BuildCustomerPermissionTreeAsync(customer, true);

            await PrepareCountriesAndStatesAsync(model);

            ViewBag.AvailableTimeZones = new List<SelectListItem>();
            foreach (var tzi in Services.DateTimeHelper.GetSystemTimeZones())
            {
                ViewBag.AvailableTimeZones.Add(new SelectListItem { Text = tzi.DisplayName, Value = tzi.Id, Selected = tzi.Id == model.TimeZoneId });
            }

            // Addresses.
            var countries = await GetAllCountriesAsync();
            var addresses = customer.Addresses
                .OrderByDescending(a => a.CreatedOnUtc)
                .ThenByDescending(a => a.Id)
                .ToList();

            foreach (var address in addresses)
            {
                var addressModel = new AddressModel();
                await address.MapAsync(addressModel, countries: countries);
                model.Addresses.Add(addressModel);
            }
        }

        private async Task<(List<CustomerRole> NewCustomerRoles, string ErrMessage)> ValidateCustomerRolesAsync(int[] selectedCustomerRoleIds, List<int> allCustomerRoleIds)
        {
            Guard.NotNull(allCustomerRoleIds, nameof(allCustomerRoleIds));

            var newCustomerRoles = new List<CustomerRole>();
            var newCustomerRoleIds = new HashSet<int>();

            if (selectedCustomerRoleIds != null)
            {
                foreach (var roleId in allCustomerRoleIds)
                {
                    if (selectedCustomerRoleIds.Contains(roleId))
                    {
                        newCustomerRoleIds.Add(roleId);
                    }
                }
            }

            if (newCustomerRoleIds.Any())
            {
                // TODO: (mh) (core) Consistency please. Decide whether to work with RoleManager.Roles or SmartDbContext.CustomerRoles.
                newCustomerRoles = await _roleManager.Roles
                    .AsNoTracking()
                    .Where(x => newCustomerRoleIds.Contains(x.Id))
                    .ToListAsync();
            }

            var isInGuestsRole = newCustomerRoles.FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Guests) != null;
            var isInRegisteredRole = newCustomerRoles.FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Registered) != null;
            var guestRole = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
            var registeredRole = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);

            // Ensure a customer is not added to both 'Guests' and 'Registered' customer roles.
            var errMessage = string.Empty;
            if (isInGuestsRole && isInRegisteredRole)
            {
                errMessage = T("Admin.Customers.CanOnlyBeCustomerOrGuest", guestRole.Name, registeredRole.Name);
            }

            // Ensure that a customer is in at least one required role ('Guests' and 'Registered').
            if (!isInGuestsRole && !isInRegisteredRole)
            {
                errMessage = T("Admin.Customers.MustBeCustomerOrGuest", guestRole.Name, registeredRole.Name);
            }

            return (newCustomerRoles, errMessage);
        }

        private void UpdateFormFields(Customer customer, CustomerModel model)
        {
            customer.AdminComment = model.AdminComment;
            customer.IsTaxExempt = model.IsTaxExempt;
            customer.Active = model.Active;
            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;

            if (_customerSettings.TitleEnabled)
            {
                customer.Title = model.Title;
            }
            if (_customerSettings.DateOfBirthEnabled)
            {
                customer.BirthDate = model.DateOfBirth;
            }
            if (_customerSettings.CompanyEnabled)
            {
                customer.Company = model.Company;
            }
            if (_dateTimeSettings.AllowCustomersToSetTimeZone)
            {
                customer.TimeZoneId = model.TimeZoneId;
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
        }

        private async Task PrepareAddressModelAsync(CustomerAddressModel model, Customer customer, Address address)
        {
            model.CustomerId = customer.Id;
            model.Username = customer.Username;

            await address.MapAsync(model.Address, countries: await GetAllCountriesAsync());
        }

        protected async Task<List<Country>> GetAllCountriesAsync()
        {
            return await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id)
                .ToListAsync();
        }

        #endregion

        #region Customers

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> List()
        {
            // Load registered customers by default.
            var registeredRole = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);

            var listModel = new CustomerListModel
            {
                UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email,
                DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled,
                CompanyEnabled = _customerSettings.CompanyEnabled,
                PhoneEnabled = _customerSettings.PhoneEnabled,
                ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled,
                SearchCustomerRoleIds = new int[] { registeredRole.Id }
            };

            // INFO: Solution approach for problem when using CustomerRoles editor template in search model. See Model comment for more info.
            //var customerRolesList = new List<SelectListItem>();
            // TODO: (mh) (core) We must call CustomerRoleController > AllCustomerRoles but without the AJAX result.
            // maybe outsource the core of AllCustomerRoles to CustomerHelper so it can also be called here to get a list of all customer roles ???
            //ViewBag.SearchCustomerRoles = customerRolesList;

            return View(listModel);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> CustomerList(GridCommand command, CustomerListModel model)
        {
            var gridModel = new GridModel<CustomerModel>();

            var searchQuery = _db.Customers
                .AsNoTracking()
                .IncludeCustomerRoles()
                .ApplyIdentFilter(model.SearchEmail, model.SearchUsername)                                                  // TODO: (mh) (core) Searchfor customernumber
                .ApplyBirthDateFilter(null, model.SearchMonthOfBirth.ToInt(), model.SearchDayOfBirth.ToInt())               // TODO: (mh) (core) Searchfor add year
                .ApplyGridCommand(command, false);              
                
            if (model.SearchCustomerRoleIds != null)
            {
                searchQuery = searchQuery.ApplyRolesFilter(model.SearchCustomerRoleIds);
            }

            if (model.SearchPhone.HasValue())
            {
                searchQuery = searchQuery.ApplyPhoneFilter(model.SearchPhone);
            }

            if (model.SearchTerm.HasValue())
            {
                searchQuery = searchQuery.ApplySearchTermFilter(model.SearchTerm);
            }

            if (model.SearchZipPostalCode.HasValue())
            {
                searchQuery = searchQuery.ApplyZipPostalCodeFilter(model.SearchZipPostalCode);
            }

            if (model.SearchActiveOnly != null)
            {
                searchQuery = searchQuery.Where(x => x.Active == model.SearchActiveOnly);
            }

            var customers = await searchQuery.ToPagedList(command).LoadAsync();

            gridModel.Rows = customers.Select(x => PrepareCustomerModelForList(x)).ToList();
            gridModel.Total = customers.TotalCount;

            return Json(gridModel);
        }

        [Permission(Permissions.Customer.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerModel();

            await PrepareCustomerModelForCreateAsync(model);

            // Set default value for creation.
            model.Active = true;

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.Create)]
        public async Task<IActionResult> Create(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            // Validate customer roles.
            var allCustomerRoleIds = await _db.CustomerRoles
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();

            var (newCustomerRoles, customerRolesError) = await ValidateCustomerRolesAsync(model.SelectedCustomerRoleIds, allCustomerRoleIds);

            if (customerRolesError.HasValue())
            {
                ModelState.AddModelError(string.Empty, customerRolesError);
            }

            if (ModelState.IsValid)
            {
                var utcNow = DateTime.UtcNow;
                var customer = new Customer
                {
                    CustomerGuid = Guid.NewGuid(),
                    Email = model.Email,
                    Username = model.Username,
                    CreatedOnUtc = utcNow,
                    LastActivityDateUtc = utcNow,
                };

                if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.AutomaticallySet && model.CustomerNumber.IsEmpty())
                {
                    customer.CustomerNumber = null;
                    // Let any NumberFormatter module handle this
                    await Services.EventPublisher.PublishAsync(new CustomerRegisteredEvent { Customer = customer });
                }
                else if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.Enabled && model.CustomerNumber.HasValue())
                {
                    var numberExists = await _db.Customers.ApplyIdentFilter(customerNumber: model.CustomerNumber).AnyAsync();
                    if (numberExists)
                    {
                        NotifyError("Common.CustomerNumberAlreadyExists");
                    }
                    else
                    {
                        customer.CustomerNumber = model.CustomerNumber;
                    }
                }

                try
                {
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

                // Form fields.
                if (ModelState.IsValid)
                {
                    UpdateFormFields(customer, model);

                    // Password.
                    if (model.Password.HasValue())
                    {
                        var changePasswordResult = await _userManager.AddPasswordAsync(customer, model.Password);

                        if (!changePasswordResult.Succeeded)
                        {
                            foreach (var changePassError in changePasswordResult.Errors)
                            {
                                NotifyError($"Error {changePassError.Code}: {changePassError.Description}");
                            }
                        }
                    }

                    // TODO: (mh) (core) Something is wrong here...
                    // Customer roles.
                    newCustomerRoles.Each(x => 
                    {
                        _db.CustomerRoleMappings.Add(new CustomerRoleMapping
                        {
                            CustomerId = customer.Id,
                            CustomerRoleId = x.Id
                        });
                    });

                    await _db.SaveChangesAsync();
                    await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, customer, form));
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewCustomer, T("ActivityLog.AddNewCustomer"), customer.Id);

                    NotifySuccess(T("Admin.Customers.Customers.Added"));
                    return continueEditing ? RedirectToAction(nameof(Edit), new { id = customer.Id }) : RedirectToAction(nameof(List));
                }
            }

            // If we got this far something failed. Redisplay form.
            await PrepareCustomerModelForCreateAsync(model);

            return View(model);
        }

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(id, false);

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerModel
            {
                Id = customer.Id,
                Email = customer.Email,
                Username = customer.Username,
                AdminComment = customer.AdminComment,
                IsTaxExempt = customer.IsTaxExempt,
                Active = customer.Active,
                TimeZoneId = customer.TimeZoneId,
                VatNumber = customer.GenericAttributes.VatNumber,
                AffiliateId = customer.AffiliateId
            };

            if (customer.AffiliateId != 0)
            {
                var affiliate = await _db.Affiliates
                    .Include(x => x.Address)
                    .FindByIdAsync(customer.AffiliateId, false);

                if (affiliate != null && affiliate.Address != null)
                {
                    model.AffiliateFullName = affiliate.Address.GetFullName();
                }
            }

            // Form fields
            model.Title = customer.Title;
            model.FirstName = customer.FirstName;
            model.LastName = customer.LastName;
            model.DateOfBirth = customer.BirthDate;
            model.Company = customer.Company;
            model.CustomerNumber = customer.CustomerNumber;
            model.Gender = customer.Gender;
            model.ZipPostalCode = customer.GenericAttributes.ZipPostalCode;
            model.CountryId = Convert.ToInt32(customer.GenericAttributes.CountryId);
            model.StreetAddress = customer.GenericAttributes.StreetAddress;
            model.StreetAddress2 = customer.GenericAttributes.StreetAddress2;
            model.City = customer.GenericAttributes.City;
            model.StateProvinceId = Convert.ToInt32(customer.GenericAttributes.StateProvinceId);
            model.Phone = customer.GenericAttributes.Phone;
            model.Fax = customer.GenericAttributes.Fax;

            model.SelectedCustomerRoleIds = customer.CustomerRoleMappings
                .Where(x => !x.IsSystemMapping)
                .Select(x => x.CustomerRoleId)
                .ToArray();

            await PrepareCustomerModelForEditAsync(model, customer);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            var customer = await _db.Customers.FindByIdAsync(model.Id);
            if (customer == null)
            {
                return NotFound();
            }

            if (customer.IsAdmin() && !Services.WorkContext.CurrentCustomer.IsAdmin())
            {
                NotifyAccessDenied();
                return RedirectToAction(nameof(Edit), new { customer.Id });
            }

            // Validate customer roles.
            var allowManagingCustomerRoles = Services.Permissions.Authorize(Permissions.Customer.EditRole);

            var allCustomerRoleIds = allowManagingCustomerRoles
                ? await _db.CustomerRoles.AsNoTracking().Select(x => x.Id).ToListAsync()
                : new List<int>();

            if (allowManagingCustomerRoles)
            {
                var tuple = await ValidateCustomerRolesAsync(model.SelectedCustomerRoleIds, allCustomerRoleIds);
                if (tuple.ErrMessage.HasValue())
                {
                    ModelState.AddModelError(string.Empty, tuple.ErrMessage);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Customer number.
                    if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
                    {
                        var numberExists = await _db.Customers.ApplyIdentFilter(customerNumber: model.CustomerNumber).AnyAsync();
                        if (model.CustomerNumber != customer.CustomerNumber && numberExists)
                        {
                            NotifyError("Common.CustomerNumberAlreadyExists");
                        }
                        else
                        {
                            customer.CustomerNumber = model.CustomerNumber;
                        }
                    }

                    if (model.Email.HasValue())
                    {
                        await _userManager.SetEmailAsync(customer, model.Email);
                    }
                    else
                    {
                        customer.Email = model.Email;
                    }

                    if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && _customerSettings.AllowUsersToChangeUsernames)
                    {
                        if (model.Username.HasValue())
                        {
                            await _userManager.SetUserNameAsync(customer, model.Username);
                        }
                        else
                        {
                            customer.Username = model.Username;
                        }
                    }

                    // VAT number.
                    if (_taxSettings.EuVatEnabled)
                    {
                        string prevVatNumber = customer.GenericAttributes.VatNumber;
                        customer.GenericAttributes.VatNumber = model.VatNumber;

                        // Set VAT number status.
                        if (model.VatNumber.HasValue())
                        {
                            if (!model.VatNumber.Equals(prevVatNumber, StringComparison.InvariantCultureIgnoreCase))
                            {
                                customer.VatNumberStatusId = (int)(await _taxService.GetVatNumberStatusAsync(model.VatNumber)).Status;
                            }
                        }
                        else
                        {
                            customer.VatNumberStatusId = (int)VatNumberStatus.Empty;
                        }
                    }

                    // Form fields.
                    UpdateFormFields(customer, model);
                    await _db.SaveChangesAsync();

                    // Customer roles.
                    if (allowManagingCustomerRoles)
                    {
                        using var scope = new DbContextScope(ctx: Services.DbContext, autoDetectChanges: false);
                        var existingMappings = customer.CustomerRoleMappings
                            .Where(x => !x.IsSystemMapping)
                            .ToMultimap(x => x.CustomerRoleId, x => x);

                        foreach (var roleId in allCustomerRoleIds)
                        {
                            if (model.SelectedCustomerRoleIds?.Contains(roleId) ?? false)
                            {
                                if (!existingMappings.ContainsKey(roleId))
                                {
                                    _db.CustomerRoleMappings.Add(new CustomerRoleMapping
                                    {
                                        CustomerId = customer.Id,
                                        CustomerRoleId = roleId
                                    });
                                }
                            }
                            else if (existingMappings.ContainsKey(roleId))
                            {
                                existingMappings[roleId].Each(x => _db.CustomerRoleMappings.Remove(x));
                            }
                        }

                        await scope.CommitAsync();
                    }

                    await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, customer, form));
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditCustomer, T("ActivityLog.EditCustomer"), customer.Id);

                    NotifySuccess(T("Admin.Customers.Customers.Updated"));
                    return continueEditing ? RedirectToAction(nameof(Edit), customer.Id) : RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message, false);
                }
            }

            // If we got this far something failed. Redisplay form.
            await PrepareCustomerModelForEditAsync(model, customer);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("changepassword"), ActionName("Edit")]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> ChangePassword(CustomerModel model)
        {
            var customer = await _db.Customers.FindByIdAsync(model.Id, false);
            if (customer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(customer, customer.Password, model.Password);

                if (!changePasswordResult.Succeeded)
                {
                    foreach (var changePassError in changePasswordResult.Errors)
                    {
                        NotifyError($"Error {changePassError.Code}: {changePassError.Description}");
                    }
                }
            }

            // No redirect here. we need validation errors to be displayed.
            return View(model);
        }

        [HttpPost]
        [FormValueRequired("markVatNumberAsValid"), ActionName("Edit")]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> MarkVatNumberAsValid(CustomerModel model)
        {
            var customer = await _db.Customers.FindByIdAsync(model.Id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.VatNumberStatusId = (int)VatNumberStatus.Valid;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), customer.Id);
        }

        [HttpPost]
        [FormValueRequired("markVatNumberAsInvalid"), ActionName("Edit")]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> MarkVatNumberAsInvalid(CustomerModel model)
        {
            var customer = await _db.Customers.FindByIdAsync(model.Id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.VatNumberStatusId = (int)VatNumberStatus.Invalid;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), customer.Id);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers.FindByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            try
            {
                _db.Customers.Remove(customer);
                
                if (customer.Email.HasValue())
                {
                    var subscriptions = await _db.NewsletterSubscriptions.Where(x => x.Email == customer.Email).ToListAsync();
                    _db.NewsletterSubscriptions.RemoveRange(subscriptions);
                    await _db.SaveChangesAsync();
                }

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteCustomer, T("ActivityLog.DeleteCustomer", customer.Id));

                NotifySuccess(T("Admin.Customers.Customers.Deleted"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception exception)
            {
                NotifyError(exception.Message);
                return RedirectToAction(nameof(Edit), new { id = customer.Id });
            }
        }

        [HttpPost]
        [Permission(Permissions.Customer.Delete)]
        public async Task<IActionResult> CustomerDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.Customers
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                foreach (var customer in toDelete)
                {
                    if (customer.Email.HasValue())
                    {
                        var subscriptions = await _db.NewsletterSubscriptions.Where(x => x.Email == customer.Email).ToListAsync();
                        _db.NewsletterSubscriptions.RemoveRange(subscriptions);
                        await _db.SaveChangesAsync();
                    }
                }

                numDeleted = toDelete.Count;

                _db.Customers.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [FormValueRequired("impersonate"), ActionName("Edit")]
        [Permission(Permissions.Customer.Impersonate)]
        public async Task<IActionResult> Impersonate(int id)
        {
            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            // Ensure that a non-admin user cannot impersonate as an administrator.
            // Otherwise, that user can simply impersonate as an administrator and gain additional administrative privileges.
            if (!Services.WorkContext.CurrentCustomer.IsAdmin() && customer.IsAdmin())
            {
                NotifyAccessDenied();
                return RedirectToAction(nameof(Edit), customer.Id);
            }

            Services.WorkContext.CurrentCustomer.GenericAttributes.ImpersonatedCustomerId = customer.Id;
            await _db.SaveChangesAsync();

            return RedirectToRoute("Homepage");
        }

        [Permission(Permissions.System.Message.Send)]
        public async Task<IActionResult> SendEmail(CustomerModel.SendEmailModel model)
        {
            var customer = await _db.Customers.FindByIdAsync(model.CustomerId);
            if (customer == null)
            {
                return NotFound();
            }

            try
            {
                if (!customer.Email.HasValue() || !customer.Email.IsEmail())
                {
                    throw new SmartException(T("Admin.Customers.Customers.SendEmail.EmailNotValid"));
                }
                
                var emailAccount = _emailAccountService.Value.GetDefaultEmailAccount() ?? throw new SmartException(T("Common.Error.NoEmailAccount"));
                var messageContext = MessageContext.Create("System.Generic", Convert.ToInt32(customer.GenericAttributes.LanguageId));

                var customModel = new NamedModelPart("Generic")
                {
                    ["ReplyTo"] = emailAccount.ToMailAddress(),
                    ["Email"] = customer.Email,
                    ["Subject"] = model.Subject,
                    ["Body"] = model.Body
                };

                // TODO: (mh) (core) Model building fails.
                // RE: Why? Please don't move on before resolving such important issues.
                await _messageFactory.CreateMessageAsync(messageContext, true, customer, Services.StoreContext.CurrentStore, customModel);

                NotifySuccess(T("Admin.Customers.Customers.SendEmail.Queued"));
            }
            catch (Exception exc)
            {
                NotifyError(exc.Message);
            }

            return RedirectToAction(nameof(Edit), new { id = customer.Id });
        }

        #endregion

        #region Reward points history

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> RewardPointsHistoryList(int customerId, GridCommand command)
        {
            var rphs = await _db.RewardPointsHistory
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                // TODO: (mh) (core) Sorting is enabled on datagrid. Why fixed sorting here? Check all other grids please.
                .OrderByDescending(rph => rph.CreatedOnUtc)
                .ThenByDescending(rph => rph.Id)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var gridModel = new GridModel<CustomerModel.RewardPointsHistoryModel>
            {
                Rows = rphs.Select(x =>
                {
                    return new CustomerModel.RewardPointsHistoryModel()
                    {
                        Id = x.Id,
                        Points = x.Points,
                        PointsBalance = x.PointsBalance,
                        Message = x.Message,
                        CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                    };
                }),
                Total = await rphs.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> RewardPointsInsert(CustomerModel.RewardPointsHistoryModel model, int customerId)
        {
            var customer = await _db.Customers.FindByIdAsync(customerId) ?? throw new ArgumentException("No customer found with the specified id");

            customer.AddRewardPointsHistoryEntry(model.Points, model.Message);
            await _db.SaveChangesAsync();
            bool success = true;

            return Json(new { success });
        }

        #endregion

        #region Orders

        [HttpPost]
        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> OrderList(int customerId, GridCommand command)
        {
            var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            var orders = await _db.Orders
                .ApplyStandardFilter(customerId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var orderModels = orders
                .Select(x =>
                {
                    allStores.TryGetValue(x.StoreId, out var store);

                    var orderModel = new CustomerModel.OrderModel
                    {
                        Id = x.Id,
                        OrderStatus = x.OrderStatus.GetLocalizedEnum(),
                        PaymentStatus = x.PaymentStatus.GetLocalizedEnum(),
                        ShippingStatus = x.ShippingStatus.GetLocalizedEnum(),
                        OrderTotal = Services.CurrencyService.PrimaryCurrency.AsMoney(x.OrderTotal),
                        StoreName = store?.Name.NullEmpty() ?? string.Empty.NaIfEmpty(),
                        CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                        EditUrl = Url.Action("Edit", "Order", new { id = x.Id })
                    };

                    return orderModel;
                })
                .ToList();

            var gridModel = new GridModel<CustomerModel.OrderModel>
            {
                Rows = orderModels,
                Total = await orders.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        #endregion

        #region Addresses

        [Permission(Permissions.Customer.EditAddress)]
        public async Task<IActionResult> AddressCreate(int customerId)
        {
            var customer = await _db.Customers.FindByIdAsync(customerId, false);
            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerAddressModel
            {
                CustomerId = customer.Id
            };

            await PrepareAddressModelAsync(model, customer, new Address());

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.EditAddress)]
        public async Task<IActionResult> AddressCreate(CustomerAddressModel model, bool continueEditing)
        {
            var customer = await _db.Customers.FindByIdAsync(model.CustomerId);
            if (customer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var address = await MapperFactory.MapAsync<AddressModel, Address>(model.Address);
                address.CreatedOnUtc = DateTime.UtcNow;

                if (address.CountryId == 0)
                {
                    address.CountryId = null;
                }
                if (address.StateProvinceId == 0)
                {
                    address.StateProvinceId = null;
                }

                customer.Addresses.Add(address);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Customers.Customers.Addresses.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(AddressEdit), new { addressId = address.Id, customerId = model.CustomerId })
                    : RedirectToAction(nameof(Edit), new { id = customer.Id });
            }

            model.CustomerId = customer.Id;

            await PrepareAddressModelAsync(model, customer, new Address());

            return View(model);
        }

        [Permission(Permissions.Customer.EditAddress)]
        public async Task<IActionResult> AddressEdit(int addressId, int customerId)
        {
            var customer = await _db.Customers.FindByIdAsync(customerId, false);
            if (customer == null)
            {
                return NotFound();
            }

            var address = await _db.Addresses.FindByIdAsync(addressId, false);
            if (address == null)
            {
                return NotFound();
            }

            var model = new CustomerAddressModel();
            await PrepareAddressModelAsync(model, customer, address);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.EditAddress)]
        public async Task<IActionResult> AddressEdit(CustomerAddressModel model, bool continueEditing)
        {
            var customer = await _db.Customers
                .Include(x => x.Addresses)
                .FindByIdAsync(model.CustomerId);

            if (customer == null)
            {
                return NotFound();
            }

            //var address = await _db.Addresses.FindByIdAsync(model.Address.Id, true);
            // TODO: (mh) (core) (perf) No good idea to load all addresses just to edit a specific one.
            var address = customer.Addresses.Where(x => x.Id == model.Address.Id).FirstOrDefault();
            if (address == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model.Address, address);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Customers.Customers.Addresses.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(AddressEdit), new { addressId = model.Address.Id, customerId = model.CustomerId })
                    : RedirectToAction(nameof(Edit), new { id = customer.Id });
            }

            await PrepareAddressModelAsync(model, customer, address);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Customer.EditAddress)]
        public async Task<IActionResult> AddressDelete(int customerId, int addressId)
        {
            var success = false;
            var customer = await _db.Customers
                .Include(x => x.Addresses)
                .FindByIdAsync(customerId);

            // TODO: (mh) (core) (perf) No good idea to load all addresses just to delete a specific one. Also, there's no need to load the customer at all.
            var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);

            if (address != null)
            {
                _db.Addresses.Remove(address);
                await _db.SaveChangesAsync();
                success = true;
            }
            
            return new JsonResult(success);
        }

        #endregion

        #region Reports

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> Reports()
        {
            ViewBag.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            ViewBag.AvailableOrderStatuses = OrderStatus.Pending.ToSelectList(false).ToList();
            ViewBag.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(false).ToList();
            ViewBag.AvailableShippingStatuses = ShippingStatus.NotYetShipped.ToSelectList(false).ToList();

            var registeredRole = await _db.CustomerRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

            var registeredCustomerReportLines = new Dictionary<string, int>
            {
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.7days").Value,
                    await GetRegisteredCustomersReportAsync(7, registeredRole.Id)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.14days").Value,
                    await GetRegisteredCustomersReportAsync(14, registeredRole.Id)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.month").Value,
                    await GetRegisteredCustomersReportAsync(30, registeredRole.Id)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.year").Value,
                    await GetRegisteredCustomersReportAsync(365, registeredRole.Id)
                }
            };

            ViewBag.RegisteredCustomerReportLines = registeredCustomerReportLines;

            return View();
        }

        private async Task<int> GetRegisteredCustomersReportAsync(int days, int registeredRoleId)
        {
            DateTime startDate = _dateTimeHelper.ConvertToUserTime(DateTime.Now).AddDays(-days);

            var customerDates = _db.Customers
                .AsNoTracking()
                .ApplyRegistrationFilter(startDate, DateTime.UtcNow)
                .ApplyRolesFilter(new[] { registeredRoleId })
                .Select(x => x.CreatedOnUtc);

            return await customerDates.CountAsync(); 
        }

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> ReportTopCustomersList(GridCommand command, TopCustomersReportModel model)
        {
            DateTime? startDateValue = (model.StartDate == null)
                ? null
                : Services.DateTimeHelper.ConvertToUtcTime(model.StartDate.Value, Services.DateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null)
                ? null
                : Services.DateTimeHelper.ConvertToUtcTime(model.EndDate.Value, Services.DateTimeHelper.CurrentTimeZone).AddDays(1);

            OrderStatus? orderStatus = model.OrderStatusId > 0 ? (OrderStatus?)model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;
            ShippingStatus? shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)model.ShippingStatusId : null;

            var items = await _db.Customers
                .AsNoTracking()
                .SelectAsTopCustomerReportLine(startDateValue, endDateValue, orderStatus, paymentStatus, shippingStatus)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var gridModel = new GridModel<TopCustomerReportLineModel>
            {
                Rows = await _customerHelper.CreateCustomerReportLineModelAsync(items),
                Total = items.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        #region Current shopping cart/ wishlist

        [Permission(Permissions.Cart.Read)]
        public async Task<IActionResult> GetCartList(int customerId, int cartTypeId, GridCommand command)
        {
            var customer = await _db.Customers
                .IncludeShoppingCart()
                .FindByIdAsync(customerId, false);

            if (customer == null)
            {
                return NotFound();
            }

            var cart = await _shoppingCartService.GetCartAsync(customer, (ShoppingCartType)cartTypeId);
            var allProducts = cart.Items
                .Select(x => x.Item.Product)
                .Union(cart.Items.Select(x => x.ChildItems).SelectMany(child => child.Select(x => x.Item.Product)))
                .ToArray();

            var models = await cart.Items.SelectAsync(async sci =>
            {
                var store = Services.StoreContext.GetStoreById(sci.Item.StoreId);
                var batchContext = _productService.CreateProductBatchContext(allProducts, store, customer, false);
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(sci, calculationOptions);
                var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                var sciModel = new ShoppingCartItemModel
                {
                    Id = sci.Item.Id,
                    Store = store != null ? store.Name : string.Empty.NaIfEmpty(),
                    ProductId = sci.Item.ProductId,
                    Quantity = sci.Item.Quantity,
                    ProductName = sci.Item.Product.Name,
                    ProductTypeName = sci.Item.Product.GetProductTypeLabel(Services.Localization),
                    ProductTypeLabelHint = sci.Item.Product.ProductTypeLabelHint,
                    UnitPrice = unitPrice.FinalPrice,
                    Total = itemSubtotal.FinalPrice,
                    UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(sci.Item.UpdatedOnUtc, DateTimeKind.Utc)
                };
                return sciModel;
            }).AsyncToList();

            var gridModel = new GridModel<ShoppingCartItemModel>
            {
                Rows = models,
                Total = cart.Items.Length
            };

            return Json(gridModel);
        }

        #endregion

        #region Activity log

        // INFO: (mh) (core) ListActivityLog > I couldn't find any occasion where this is used. Legacy?

        #endregion

        #region GDPR

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> Export(int id /* customerId */)
        {
            var customer = await _db.Customers.FindByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var data = await _gdprTool.Value.ExportCustomerAsync(customer);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);

            return File(Encoding.UTF8.GetBytes(json), "application/json", "customer-{0}.json".FormatInvariant(customer.Id));
        }

        #endregion
    }
}
