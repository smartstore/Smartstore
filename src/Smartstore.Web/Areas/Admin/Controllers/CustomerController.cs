using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Cart;
using Smartstore.Admin.Models.Customers;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
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
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly IMessageFactory _messageFactory;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ITaxService _taxService;
        private readonly Lazy<IEmailAccountService> _emailAccountService;
        private readonly Lazy<IGdprTool> _gdprTool;
        private readonly Lazy<IGeoCountryLookup> _geoCountryLookup;
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<IShippingService> _shippingService;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CustomerController(
            SmartDbContext db,
            ICustomerService customerService,
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            IMessageFactory messageFactory,
            DateTimeSettings dateTimeSettings,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            CustomerSettings customerSettings,
            ITaxService taxService,
            Lazy<IEmailAccountService> emailAccountService,
            Lazy<IGdprTool> gdprTool,
            Lazy<IGeoCountryLookup> geoCountryLookup,
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<IShippingService> shippingService,
            Lazy<IPaymentService> paymentService,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _customerService = customerService;
            _userManager = userManager;
            _signInManager = signInManager;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _messageFactory = messageFactory;
            _dateTimeSettings = dateTimeSettings;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _customerSettings = customerSettings;
            _taxService = taxService;
            _emailAccountService = emailAccountService;
            _gdprTool = gdprTool;
            _geoCountryLookup = geoCountryLookup;
            _shoppingCartService = shoppingCartService;
            _shippingService = shippingService;
            _paymentService = paymentService;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #region Utilities

        private async Task<List<CustomerModel.AssociatedExternalAuthModel>> GetAssociatedExternalAuthRecords(Customer customer)
        {
            Guard.NotNull(customer);

            await _db.LoadCollectionAsync(customer, x => x.ExternalAuthenticationRecords);

            var authSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var authProviders = _providerManager.GetAllProviders<IExternalAuthenticationMethod>()
                .ToDictionarySafe(x => x.Metadata.SystemName, x => x, StringComparer.OrdinalIgnoreCase);

            return customer.ExternalAuthenticationRecords
                .Select(x =>
                {
                    var methodName = authProviders.TryGetValue(x.ProviderSystemName, out var provider)
                        ? _moduleManager.GetLocalizedFriendlyName(provider.Metadata).NullEmpty() ?? provider.Metadata.FriendlyName.NullEmpty()
                        : null;

                    if (methodName == null)
                    {
                        // Method has a system name mapping.
                        var authScheme = authSchemes.FirstOrDefault(scheme => x.ProviderSystemName.Contains(scheme.Name, StringComparison.OrdinalIgnoreCase));
                        methodName = authScheme?.Name;
                    }

                    return new CustomerModel.AssociatedExternalAuthModel
                    {
                        Id = x.Id,
                        Email = x.Email,
                        ExternalIdentifier = x.ExternalIdentifier,
                        AuthMethodName = methodName ?? x.ProviderSystemName
                    };
                })
                .ToList();
        }

        private async Task PrepareCustomerModel(CustomerModel model, Customer customer)
        {
            var dtHelper = Services.DateTimeHelper;

            MiniMapper.Map(_customerSettings, model);

            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;
            model.AllowManagingCustomerRoles = await Services.Permissions.AuthorizeAsync(Permissions.Customer.EditRole);
            model.CustomerNumberEnabled = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;

            if (customer != null)
            {
                await _db.LoadCollectionAsync(customer, x => x.Addresses, false, q => q
                    .Include(x => x.Country)
                    .Include(x => x.StateProvince));

                model.Id = customer.Id;
                model.IsGuest = customer.IsGuest();
                model.Email = customer.Email;
                model.Username = customer.Username;
                model.AdminComment = customer.AdminComment;
                model.IsTaxExempt = customer.IsTaxExempt;
                model.Active = customer.Active;
                model.TimeZoneId = customer.TimeZoneId;
                model.VatNumber = customer.GenericAttributes.VatNumber;
                model.AffiliateId = customer.AffiliateId;
                model.Deleted = customer.Deleted;
                model.Title = customer.Title;
                model.FirstName = customer.FirstName;
                model.LastName = customer.LastName;
                model.FullName = customer.GetFullName();
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
                model.DateOfBirth = customer.BirthDate;
                model.DisplayVatNumber = _taxSettings.EuVatEnabled;
                model.DisplayRewardPointsHistory = _rewardPointsSettings.Enabled;
                model.VatNumberStatusNote = ((VatNumberStatus)customer.VatNumberStatusId).GetLocalizedEnum();
                model.CreatedOn = dtHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc);
                model.LastActivityDate = dtHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc);
                model.LastIpAddress = customer.LastIpAddress;
                model.LastVisitedPage = customer.GenericAttributes.LastVisitedPage;
                model.DisplayProfileLink = _customerSettings.AllowViewingProfiles && !model.IsGuest;
                model.AssociatedExternalAuthRecords = await GetAssociatedExternalAuthRecords(customer);
                model.PermissionTree = await Services.Permissions.BuildCustomerPermissionTreeAsync(customer, true);
                model.HasOrders = await _db.Orders.AnyAsync(x => x.CustomerId == customer.Id);

                model.Addresses = new()
                {
                    Id = customer.Id,
                    Addresses = await customer.Addresses.MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled)
                };

                model.SelectedCustomerRoleIds = customer.CustomerRoleMappings
                    .Where(x => !x.IsSystemMapping)
                    .Select(x => x.CustomerRoleId)
                    .ToArray();

                var affiliate = await _db.Affiliates
                    .Include(x => x.Address)
                    .FindByIdAsync(customer.AffiliateId, false);

                model.AffiliateFullName = affiliate?.Address?.GetFullName();
            }
            else
            {
                // Set default values for creation.
                model.Active = true;

                if (model.SelectedCustomerRoleIds == null || model.SelectedCustomerRoleIds.Length == 0)
                {
                    var role = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
                    model.SelectedCustomerRoleIds = [role.Id];
                }
            }

            // ViewBag.
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            ViewBag.AvailableTimeZones = dtHelper.GetSystemTimeZones()
                .ToSelectListItems(model.TimeZoneId.NullEmpty() ?? dtHelper.DefaultStoreTimeZone.Id);

            // Countries and state provinces.
            if (_customerSettings.CountryEnabled && model.CountryId > 0)
            {
                if (_customerSettings.StateProvinceEnabled)
                {
                    var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync((int)model.CountryId);
                    ViewBag.AvailableStates = stateProvinces.ToSelectListItems(model.StateProvinceId) ?? new List<SelectListItem>
                    {
                        new() { Text = T("Address.OtherNonUS"), Value = "0" }
                    };
                }
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                var shippingMethods = await _shippingService.Value.GetAllShippingMethodsAsync();
                var paymentProviders = await _paymentService.Value.LoadActivePaymentProvidersAsync();
                var preferredShippingMethodId = customer?.GenericAttributes.PreferredShippingOption?.ShippingMethodId ?? 0;
                var preferredPaymentMethod = customer?.GenericAttributes.PreferredPaymentMethod;

                ViewBag.ShippingMethods = shippingMethods
                    .Select(x => new SelectListItem
                    {
                        Text = x.GetLocalized(y => y.Name),
                        Value = x.Id.ToString(),
                        Selected = x.Id == preferredShippingMethodId
                    })
                    .ToList();

                ViewBag.PaymentMethods = paymentProviders
                    .Where(x => !x.Value.RequiresPaymentSelection)
                    .Select(x => x.Metadata)
                    .Select(x => new SelectListItem
                    {
                        Text = _moduleManager.GetLocalizedFriendlyName(x) ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                        Value = x.SystemName,
                        Selected = x.SystemName.EqualsNoCase(preferredPaymentMethod)
                    })
                    .ToList();
            }
        }

        private async Task<(List<CustomerRole> NewCustomerRoles, string ErrMessage)> ValidateCustomerRoles(
            int[] selectedCustomerRoleIds,
            List<int> allCustomerRoleIds)
        {
            Guard.NotNull(allCustomerRoleIds);

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

            if (newCustomerRoleIds.Count > 0)
            {
                newCustomerRoles = await _db.CustomerRoles
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

        private void MapCustomerModel(CustomerModel from, Customer to)
        {
            to.AdminComment = from.AdminComment;
            to.IsTaxExempt = from.IsTaxExempt;
            to.Active = from.Active;
            to.FirstName = from.FirstName;
            to.LastName = from.LastName;

            if (_customerSettings.TitleEnabled)
            {
                to.Title = from.Title;
            }
            if (_customerSettings.DateOfBirthEnabled)
            {
                to.BirthDate = from.DateOfBirth;
            }
            if (_customerSettings.CompanyEnabled)
            {
                to.Company = from.Company;
            }
            if (_dateTimeSettings.AllowCustomersToSetTimeZone)
            {
                to.TimeZoneId = from.TimeZoneId;
            }
            if (_customerSettings.GenderEnabled)
            {
                to.Gender = from.Gender;
            }
            if (_customerSettings.StreetAddressEnabled)
            {
                to.GenericAttributes.StreetAddress = from.StreetAddress;
            }
            if (_customerSettings.StreetAddress2Enabled)
            {
                to.GenericAttributes.StreetAddress2 = from.StreetAddress2;
            }
            if (_customerSettings.ZipPostalCodeEnabled)
            {
                to.GenericAttributes.ZipPostalCode = from.ZipPostalCode;
            }
            if (_customerSettings.CityEnabled)
            {
                to.GenericAttributes.City = from.City;
            }
            if (_customerSettings.CountryEnabled)
            {
                to.GenericAttributes.CountryId = from.CountryId;
            }
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            {
                to.GenericAttributes.StateProvinceId = from.StateProvinceId;
            }
            if (_customerSettings.PhoneEnabled)
            {
                to.GenericAttributes.Phone = from.Phone;
            }
            if (_customerSettings.FaxEnabled)
            {
                to.GenericAttributes.Fax = from.Fax;
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                to.GenericAttributes.PreferredShippingOption = new() { ShippingMethodId = from.PreferredShippingMethodId ?? 0 };
                to.GenericAttributes.PreferredPaymentMethod = from.PreferredPaymentMethod.NullEmpty();
            }
        }

        private void AddModelErrors(IdentityResult result, string key)
        {
            if (!result.Succeeded)
            {
                result.Errors
                    .Select(x => x.Description)
                    .Distinct()
                    .Each(x => ModelState.AddModelError(key, x));
            }
        }

        private async Task PrepareAddressModelAsync(CustomerAddressModel model, Customer customer, Address address)
        {
            await address.MapAsync(model.Address, customer, _shoppingCartSettings.QuickCheckoutEnabled);

            model.CustomerId = customer.Id;
            model.Username = customer.Username;
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

            return View(listModel);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> CustomerList(GridCommand command, CustomerListModel model)
        {
            var searchQuery = _db.Customers
                .AsNoTracking()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .IncludeCustomerRoles()
                .ApplyIdentFilter(model.SearchEmail, model.SearchUsername, model.SearchCustomerNumber)
                .ApplyBirthDateFilter(model.SearchYearOfBirth.ToInt(), model.SearchMonthOfBirth.ToInt(), model.SearchDayOfBirth.ToInt());

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

            var customers = await searchQuery
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            string guestStr = T("Admin.Customers.Guest");
            var dtHelper = Services.DateTimeHelper;

            var rows = customers
                .Select(x => new CustomerModel
                {
                    Id = x.Id,
                    Email = x.Email.HasValue() ? x.Email : (x.IsGuest() ? guestStr : StringExtensions.NotAvailable),
                    Username = x.Username,
                    FullName = x.GetFullName(),
                    Company = x.Company,
                    CustomerNumber = x.CustomerNumber,
                    ZipPostalCode = x.GenericAttributes.ZipPostalCode,
                    Active = x.Active,
                    Phone = x.GenericAttributes.Phone,
                    CustomerRoleNames = string.Join(", ", x.CustomerRoleMappings.Select(x => x.CustomerRole).Select(x => x.Name)),
                    CreatedOn = dtHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    LastActivityDate = dtHelper.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action(nameof(Edit), "Customer", new { id = x.Id }),
                    IsTaxExempt = x.IsTaxExempt,
                    LastIpAddress = x.LastIpAddress,
                    DateOfBirth = x.BirthDate.HasValue ? dtHelper.ConvertToUserTime(x.BirthDate.Value, DateTimeKind.Utc) : null,
                    Gender = x.Gender,
                    VatNumber = x.GenericAttributes.VatNumber
                })
                .ToList();

            return Json(new GridModel<CustomerModel>
            {
                Rows = rows,
                Total = await customers.GetTotalCountAsync()
            });
        }

        [Permission(Permissions.Customer.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerModel();

            await PrepareCustomerModel(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.Create)]
        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> Create(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Email = model.Email,
                Username = model.Username,
                PasswordFormat = _customerSettings.DefaultPasswordFormat,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow
            };

            // Validate customer roles.
            var allCustomerRoleIds = await _db.CustomerRoles.Select(x => x.Id).ToListAsync();
            var (newCustomerRoles, customerRolesError) = await ValidateCustomerRoles(model.SelectedCustomerRoleIds, allCustomerRoleIds);
            if (customerRolesError.HasValue())
            {
                ModelState.AddModelError(nameof(model.SelectedCustomerRoleIds), customerRolesError);
            }

            // Customer number.
            if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.AutomaticallySet && model.CustomerNumber.IsEmpty())
            {
                // Let any NumberFormatter module handle this.
                await Services.EventPublisher.PublishAsync(new CustomerRegisteredEvent { Customer = customer });
            }
            else if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.Enabled && model.CustomerNumber.HasValue())
            {
                if (await _db.Customers.IgnoreQueryFilters().AnyAsync(x => x.CustomerNumber == model.CustomerNumber))
                {
                    NotifyError(T("Common.CustomerNumberAlreadyExists"));
                }
                else
                {
                    customer.CustomerNumber = model.CustomerNumber;
                }
            }

            if (ModelState.IsValid)
            {
                var createResult = await _userManager.CreateAsync(customer, model.Password);
                if (createResult.Succeeded)
                {
                    MapCustomerModel(model, customer);

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

                    return continueEditing
                        ? RedirectToAction(nameof(Edit), new { id = customer.Id })
                        : RedirectToAction(nameof(List));
                }
                else
                {
                    AddModelErrors(createResult, nameof(model.Password));
                }
            }

            // If we got this far something failed. Redisplay form.
            await PrepareCustomerModel(model, null);

            return View(model);
        }

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers
                .AsSplitQuery()
                .IncludeCustomerRoles()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .Include(x => x.Addresses)
                .Include(x => x.ExternalAuthenticationRecords)
                .FindByIdAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerModel();
            await PrepareCustomerModel(model, customer);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Customer.Update)]
        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(model.Id);

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
                : [];

            if (allowManagingCustomerRoles)
            {
                var (_, errMessage) = await ValidateCustomerRoles(model.SelectedCustomerRoleIds, allCustomerRoleIds);
                if (errMessage.HasValue())
                {
                    ModelState.AddModelError(string.Empty, errMessage);
                }
            }

            // INFO: update email and username requires SaveChangesAttribute to be set to 'false'.
            var newEmail = model.Email.TrimSafe();
            var newUsername = model.Username.TrimSafe();

            // Email.
            if (ModelState.IsValid && !newEmail.EqualsNoCase(customer.Email))
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(customer, newEmail);
                var result = await _userManager.ChangeEmailAsync(customer, newEmail, token);
                AddModelErrors(result, nameof(model.Email));
            }

            // Username.
            if (ModelState.IsValid
                && _customerSettings.CustomerLoginType != CustomerLoginType.Email
                && _customerSettings.AllowUsersToChangeUsernames
                && !newUsername.EqualsNoCase(customer.Username))
            {
                var result = await _userManager.SetUserNameAsync(customer, newUsername);
                AddModelErrors(result, nameof(model.Username));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Customer number.
                    if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
                    {
                        if (model.CustomerNumber.HasValue()
                            && model.CustomerNumber != customer.CustomerNumber
                            && await _db.Customers.IgnoreQueryFilters().AnyAsync(x => x.CustomerNumber == model.CustomerNumber))
                        {
                            NotifyError(T("Common.CustomerNumberAlreadyExists"));
                        }
                        else
                        {
                            customer.CustomerNumber = model.CustomerNumber;
                        }
                    }

                    // VAT number.
                    if (_taxSettings.EuVatEnabled)
                    {
                        var prevVatNumber = customer.GenericAttributes.VatNumber;
                        customer.GenericAttributes.VatNumber = model.VatNumber;

                        // Set VAT number status.
                        if (model.VatNumber.HasValue())
                        {
                            if (!model.VatNumber.Equals(prevVatNumber, StringComparison.InvariantCultureIgnoreCase))
                            {
                                var response = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                                customer.VatNumberStatusId = (int)response.Status;

                                if (response.Exception != null)
                                {
                                    NotifyError("Checking the VAT number with the VAT validation web service threw this exception: " + response.Exception.Message);
                                }
                            }
                        }
                        else
                        {
                            customer.VatNumberStatusId = (int)VatNumberStatus.Empty;
                        }
                    }

                    // Model properties.
                    MapCustomerModel(model, customer);

                    var updateResult = await _userManager.UpdateAsync(customer);

                    if (updateResult.Succeeded)
                    {
                        // Customer roles.
                        if (allowManagingCustomerRoles)
                        {
                            using var scope = new DbContextScope(db: Services.DbContext, autoDetectChanges: false);
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

                        return continueEditing
                            ? RedirectToAction(nameof(Edit), customer.Id)
                            : RedirectToAction(nameof(List));
                    }
                    else if (!model.IsGuest)
                    {
                        AddModelErrors(updateResult, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message, false);
                }
            }

            // If we got this far something failed. Redisplay form.
            await PrepareCustomerModel(model, customer);

            return View(model);
        }

        // AJAX.
        [HttpPost]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> ChangePassword(CustomerModel.ChangePasswordModel model)
        {
            ViewBag.ShowFormOnly = true;

            var customer = await _db.Customers.FindByIdAsync(model.Id);
            if (customer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                IdentityResult passwordResult;
                if (await _userManager.HasPasswordAsync(customer))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(customer);
                    passwordResult = await _userManager.ResetPasswordAsync(customer, token, model.Password);
                }
                else
                {
                    passwordResult = await _userManager.AddPasswordAsync(customer, model.Password);
                }

                if (passwordResult.Succeeded)
                {
                    return new EmptyResult();
                }

                AddModelErrors(passwordResult, string.Empty);
            }

            return PartialView("_ChangePasswordPopup", model);
        }

        [HttpPost]
        [FormValueRequired("removeAffiliateAssignment"), ActionName("Edit")]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> RemoveAffiliateAssignment(int id)
        {
            var customer = await _db.Customers.FindByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.AffiliateId = 0;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), customer.Id);
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

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteCustomer, T("ActivityLog.DeleteCustomer", customer.Id));
                NotifySuccess(T("Admin.Customers.Customers.Deleted"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
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
                var toDelete = await _db.Customers.GetManyAsync(ids, true);

                _db.Customers.RemoveRange(toDelete);
                await _db.SaveChangesAsync();

                numDeleted = toDelete.Count;
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
                if (!customer.Email.IsEmail())
                {
                    throw new InvalidOperationException(T("Admin.Customers.Customers.SendEmail.EmailNotValid"));
                }

                var emailAccount = _emailAccountService.Value.GetDefaultEmailAccount() ?? throw new Exception(T("Common.Error.NoEmailAccount"));
                var messageContext = MessageContext.Create("System.Generic", Convert.ToInt32(customer.GenericAttributes.LanguageId));

                var customModel = new NamedModelPart("Generic")
                {
                    ["ReplyTo"] = emailAccount.ToMailAddress().ToString(),
                    ["Email"] = customer.Email,
                    ["Subject"] = model.Subject,
                    ["Body"] = model.Body
                };

                await _messageFactory.CreateMessageAsync(messageContext, true, customer, Services.StoreContext.CurrentStore, customModel);

                NotifySuccess(T("Admin.Customers.Customers.SendEmail.Queued"));
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
            }

            return RedirectToAction(nameof(Edit), new { id = customer.Id });
        }

        #endregion

        #region Online customers

        public IActionResult OnlineCustomers()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> OnlineCustomersList(GridCommand command)
        {
            var customers = await _db.Customers
                .AsNoTracking()
                .IncludeCustomerRoles()
                .Where(x => !x.IsSystemAccount)
                .ApplyOnlineCustomersFilter(_customerSettings.OnlineCustomerMinutes)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var guestStr = T("Admin.Customers.Guest").Value;

            var customerModels = customers
                .Select(x => new OnlineCustomerModel
                {
                    Id = x.Id,
                    CustomerInfo = x.IsRegistered() ? x.Email : guestStr,
                    CustomerNumber = x.CustomerNumber,
                    Active = x.Active,
                    LastIpAddress = x.LastIpAddress,
                    Location = _geoCountryLookup.Value.LookupCountry(x.LastIpAddress)?.Name.EmptyNull(),
                    LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
                    LastVisitedPage = x.GenericAttributes.LastVisitedPage,
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("Edit", "Customer", new { id = x.Id })
                })
                .ToList();

            var gridModel = new GridModel<OnlineCustomerModel>
            {
                Rows = customerModels,
                Total = await customers.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        #endregion

        #region Reward points history

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> RewardPointsHistoryList(int customerId, GridCommand command)
        {
            var rphs = await _db.RewardPointsHistory
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(rph => rph.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var gridModel = new GridModel<CustomerModel.RewardPointsHistoryModel>
            {
                Rows = rphs.Select(x =>
                {
                    return new CustomerModel.RewardPointsHistoryModel
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

        #region Addresses

        [Permission(Permissions.Customer.ReadAddress)]
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
        [Permission(Permissions.Customer.CreateAddress)]
        public async Task<IActionResult> AddressCreate(CustomerAddressModel model, bool continueEditing)
        {
            var customer = await _db.Customers.FindByIdAsync(model.CustomerId);
            if (customer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var address = new Address();
                await model.Address.MapAsync(address);

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

        [Permission(Permissions.Customer.ReadAddress)]
        public async Task<IActionResult> AddressEdit(int customerId, int addressId)
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
            var customer = await _db.Customers.FindByIdAsync(model.CustomerId, false);
            if (customer == null)
            {
                return NotFound();
            }

            var address = await _db.Addresses.FindByIdAsync(model.Address.Id, true);
            if (address == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await model.Address.MapAsync(address, customer, _shoppingCartSettings.QuickCheckoutEnabled);
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
        public async Task<IActionResult> SetDefaultAddress(int customerId, int addressId)
        {
            var customer = await _db.Customers
                .Include(x => x.Addresses)
                .ThenInclude(x => x.Country)
                .FindByIdAsync(customerId);

            var address = customer?.Addresses?.FirstOrDefault(x => x.Id == addressId);
            if (address == null)
            {
                return NotFound();
            }

            var billingAllowed = address.Country?.AllowsBilling ?? true;
            var shippingAllowed = address.Country?.AllowsShipping ?? true;

            if (billingAllowed && shippingAllowed)
            {
                customer.GenericAttributes.DefaultBillingAddressId = address.Id;
                customer.GenericAttributes.DefaultShippingAddressId = address.Id;
                await _db.SaveChangesAsync();
            }
            else
            {
                if (!billingAllowed)
                {
                    NotifyError(T("Order.CountryNotAllowedForBilling", address.Country?.GetLocalized(x => x.Name)));
                }
                if (!shippingAllowed)
                {
                    NotifyError(T("Order.CountryNotAllowedForShipping", address.Country?.GetLocalized(x => x.Name)));
                }
            }

            var model = new CustomerModel.AddressesModel
            {
                Id = customer.Id,
                Addresses = await customer.Addresses.MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled)
            };

            return PartialView("_Addresses", model);
        }

        [HttpPost]
        [Permission(Permissions.Customer.DeleteAddress)]
        public async Task<IActionResult> AddressDelete(int customerId, int addressId)
        {
            var customer = await _db.Customers
                .Include(x => x.Addresses)
                .FindByIdAsync(customerId);

            var address = customer?.Addresses?.FirstOrDefault(x => x.Id == addressId);
            if (address == null)
            {
                return NotFound();
            }

            customer.RemoveAddress(address);
            _db.Addresses.Remove(address);
            await _db.SaveChangesAsync();

            var model = new CustomerModel.AddressesModel
            {
                Id = customer.Id,
                Addresses = await customer.Addresses.MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled)
            };

            return PartialView("_Addresses", model);
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

            var registeredRoleId = await _db.CustomerRoles
                .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            ViewBag.RegisteredCustomerReportLines = new Dictionary<string, int>
            {
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.7days"),
                    await GetRegisteredCustomersReport(7)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.14days"),
                    await GetRegisteredCustomersReport(14)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.month"),
                    await GetRegisteredCustomersReport(30)
                },
                {
                    T("Admin.Customers.Reports.RegisteredCustomers.Fields.Period.year"),
                    await GetRegisteredCustomersReport(365)
                }
            };

            return View();

            async Task<int> GetRegisteredCustomersReport(int days)
            {
                var startDate = Services.DateTimeHelper.ConvertToUserTime(DateTime.Now).AddDays(-days);

                return await _db.Customers
                    .ApplyRolesFilter(new[] { registeredRoleId })
                    .ApplyRegistrationFilter(startDate, null)
                    .CountAsync();
            }
        }

        [Permission(Permissions.Customer.Read)]
        public async Task<IActionResult> ReportTopCustomersList(GridCommand command, TopCustomersReportModel model)
        {
            var dtHelper = Services.DateTimeHelper;

            DateTime? startDate = model.StartDate != null
                ? dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone)
                : null;

            DateTime? endDate = model.EndDate != null
                ? dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1)
                : null;

            var orderStatusIds = model.OrderStatusId > 0 ? new[] { model.OrderStatusId } : null;
            var paymentStatusIds = model.PaymentStatusId > 0 ? new[] { model.PaymentStatusId } : null;
            var shippingStatusIds = model.ShippingStatusId > 0 ? new[] { model.ShippingStatusId } : null;

            var orderQuery = _db.Orders
                .Where(x => !x.Customer.Deleted)
                .ApplyStatusFilter(orderStatusIds, paymentStatusIds, shippingStatusIds)
                .ApplyAuditDateFilter(startDate, endDate);

            var reportLines = await orderQuery
                .SelectAsTopCustomerReportLine()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var gridModel = new GridModel<TopCustomerReportLineModel>
            {
                Rows = await reportLines.MapAsync(_db),
                Total = await reportLines.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        #endregion

        #region Current shopping cart\wishlist

        [Permission(Permissions.Cart.Read)]
        public async Task<IActionResult> GetCartList(int customerId, int cartTypeId)
        {
            var customer = await _db.Customers
                .IncludeShoppingCart()
                .FindByIdAsync(customerId);

            if (customer == null)
            {
                return NotFound();
            }

            var cart = await _shoppingCartService.Value.GetCartAsync(customer, (ShoppingCartType)cartTypeId, 0, null);
            var models = await cart.MapAsync();

            var gridModel = new GridModel<ShoppingCartItemModel>
            {
                Rows = models,
                Total = cart.Items.Length
            };

            return Json(gridModel);
        }

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
            var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects
            });

            return File(json.GetBytes(), "application/json", "customer-{0}.json".FormatInvariant(customer.Id));
        }

        #endregion
    }
}
