using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Net;
using Smartstore.Threading;

namespace Smartstore.Web
{
    public partial class WebWorkContext : IWorkContext
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILanguageResolver _languageResolver;
        private readonly IStoreContext _storeContext;
        private readonly ICustomerService _customerService;
        private readonly Lazy<ITaxService> _taxService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly PrivacySettings _privacySettings;
        private readonly TaxSettings _taxSettings;
        private readonly ICacheManager _cache;
        private readonly IUserAgent _userAgent;
        private readonly IWebHelper _webHelper;
        private readonly IGeoCountryLookup _geoCountryLookup;

        // KeyItem1 = CustomerId, KeyItem2 = StoreId
        private readonly Dictionary<(int, int), TaxDisplayType> _taxDisplayTypes = new();

        private Customer _customer;
        private Language _language;
        private Currency _currency;
        private Customer _impersonator;
        private bool? _isAdminArea;

        public WebWorkContext(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            ILanguageResolver languageResolver,
            IStoreContext storeContext,
            ICustomerService customerService,
            Lazy<ITaxService> taxService,
            Lazy<ICurrencyService> currencyService,
            PrivacySettings privacySettings,
            TaxSettings taxSettings,
            ICacheManager cache,
            IUserAgent userAgent,
            IWebHelper webHelper,
            IGeoCountryLookup geoCountryLookup)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _languageResolver = languageResolver;
            _storeContext = storeContext;
            _customerService = customerService;
            _privacySettings = privacySettings;
            _taxSettings = taxSettings;
            _taxService = taxService;
            _currencyService = currencyService;
            _userAgent = userAgent;
            _cache = cache;
            _webHelper = webHelper;
            _geoCountryLookup = geoCountryLookup;
        }

        public async Task InitializeAsync()
        {
            if (_customer == null)
            {
                _customer = await ResolveCurrentCustomerAsync();
            }

            if (_language == null)
            {
                _language = await ResolveWorkingLanguageAsync(_customer);
            }

            if (_currency == null)
            {
                _currency = await ResolveWorkingCurrencyAsync(_customer);
            }
        }

        #region Customer

        public Customer CurrentCustomer
        {
            get
            {
                if (_customer == null)
                {
                    InitializeAsync().Await();
                }

                return _customer;
            }
            set => _customer = value;
        }

        protected virtual async Task<Customer> ResolveCurrentCustomerAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Is system account?
            if ((await TryGetSystemAccountAsync(httpContext)).Out(out var customer))
            {
                // Get out quickly. Bots tend to overstress the shop.
                return customer;
            }

            // Registered/Authenticated customer?
            customer = await _customerService.GetAuthenticatedCustomerAsync();

            // impersonate user if required (currently used for 'phone order' support)
            if (customer != null)
            {
                var impersonatedCustomerId = customer.GenericAttributes.ImpersonatedCustomerId;
                if (impersonatedCustomerId > 0)
                {
                    var impersonatedCustomer = await _db.Customers
                        .IncludeCustomerRoles()
                        .FindByIdAsync(impersonatedCustomerId.Value);

                    if (impersonatedCustomer != null && !impersonatedCustomer.Deleted && impersonatedCustomer.Active)
                    {
                        // set impersonated customer
                        _impersonator = customer;
                        customer = impersonatedCustomer;
                    }
                }
            }

            // Load guest customer
            if (customer == null || customer.Deleted || !customer.Active)
            {
                customer = await GetGuestCustomerAsync(httpContext);
            }

            return customer;
        }

        protected async Task<AsyncOut<Customer>> TryGetSystemAccountAsync(HttpContext context)
        {
            // Never check whether customer is deleted/inactive in this method.
            // System accounts should neither be deletable nor activatable, they are mandatory.

            Customer customer = null;

            // check whether request is made by a background task
            // in this case return built-in customer record for background task
            if (context != null && context.Request.IsCalledByTaskScheduler())
            {
                customer = await _customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.BackgroundTask);
            }

            // check whether request is made by a search engine
            // in this case return built-in customer record for search engines 
            if (customer == null && _userAgent.IsBot)
            {
                customer = await _customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.SearchEngine);
            }

            // check whether request is made by the PDF converter
            // in this case return built-in customer record for the converter
            if (customer == null && _userAgent.IsPdfConverter)
            {
                customer = await _customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.PdfConverter);
            }

            return new AsyncOut<Customer>(customer != null, customer);
        }

        protected virtual async Task<Customer> GetGuestCustomerAsync(HttpContext context)
        {
            Customer customer = null;

            var visitorCookie = context?.Request?.Cookies[CookieNames.Visitor];
            if (visitorCookie == null)
            {
                // No anonymous visitor cookie yet. Try to identify anyway (by IP and UserAgent)
                customer = await _customerService.FindGuestCustomerByClientIdentAsync(maxAgeSeconds: 180);
            }
            else if (Guid.TryParse(visitorCookie, out var customerGuid))
            {
                // Cookie present. Try to load guest customer by it's value.
                customer = await _db.Customers
                    .IncludeShoppingCart()
                    .IncludeCustomerRoles()
                    .Where(c => c.CustomerGuid == customerGuid)
                    .FirstOrDefaultAsync();
            }

            if (customer == null || customer.Deleted || !customer.Active || customer.IsRegistered())
            {
                // No record yet or account deleted/deactivated.
                // Also dont' treat registered customers as guests.
                // Create new record in these cases.
                customer = await _customerService.CreateGuestCustomerAsync();
            }

            if (context != null)
            {
                var cookieExpiry = customer.CustomerGuid == Guid.Empty
                    ? DateTime.Now.AddMonths(-1)
                    : DateTime.Now.AddHours(24 * 365); // TODO make configurable

                // Set visitor cookie
                var cookieOptions = new CookieOptions
                {
                    Expires = cookieExpiry,
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = _webHelper.IsCurrentConnectionSecured(),
                    SameSite = SameSiteMode.Lax
                };

                // INFO: Global OnAppendCookie does not always run for visitor cookie.
                if (cookieOptions.Secure)
                {
                    cookieOptions.SameSite = _privacySettings.SameSiteMode;
                }

                if (context.Request.PathBase.HasValue)
                {
                    cookieOptions.Path = context.Request.PathBase;
                }

                var cookies = context.Response.Cookies;
                try
                {
                    cookies.Delete(CookieNames.Visitor, cookieOptions);
                }
                finally
                {
                    cookies.Append(CookieNames.Visitor, customer.CustomerGuid.ToString(), cookieOptions);
                }
            }

            return customer;
        }

        public Customer CurrentImpersonator => _impersonator;

        #endregion

        #region Language

        public Language WorkingLanguage
        {
            get
            {
                if (_language == null)
                {
                    InitializeAsync().Await();
                }

                return _language;
            }
            set
            {
                if (value?.Id != _language?.Id)
                {
                    SetCustomerLanguageAsync(value?.Id, false).Await();
                    _language = value;
                }
            }
        }

        protected virtual async Task<Language> ResolveWorkingLanguageAsync(Customer customer)
        {
            // Resolve the current working language
            var language = await _languageResolver.ResolveLanguageAsync(customer, _httpContextAccessor.HttpContext);

            // Set language if current customer langid does not match resolved language id
            var customerAttributes = customer.GenericAttributes;
            if (customerAttributes.LanguageId != language.Id)
            {
                await SetCustomerLanguageAsync(language.Id, true);
            }

            return language;
        }

        private async Task SetCustomerLanguageAsync(int? languageId, bool async)
        {
            var customer = CurrentCustomer;

            if (customer == null || customer.IsSystemAccount)
            {
                return;
            }

            var customerAttributes = customer.GenericAttributes;
            customerAttributes.LanguageId = languageId;

            if (async)
            {
                await customerAttributes.SaveChangesAsync();
            }
            else
            {
                customerAttributes.SaveChanges();
            }
        }

        #endregion

        #region Currency

        public Currency WorkingCurrency
        {
            get
            {
                if (_currency == null)
                {
                    InitializeAsync().Await();
                }

                return _currency;
            }
            set
            {
                if (value?.Id != _currency?.Id)
                {
                    SetCustomerCurrencyAsync(value?.Id, false).Await();
                    _currency = value;
                }
            }
        }

        protected virtual async Task<Currency> ResolveWorkingCurrencyAsync(Customer customer)
        {
            var query = _db.Currencies.AsNoTracking();

            Currency currency = null;

            // Return primary store currency when we're in admin area/mode
            if (IsAdminArea)
            {
                currency = _currencyService.Value.PrimaryCurrency;
            }

            if (currency == null)
            {
                // Find current customer currency
                var storeCurrenciesMap = query.ApplyStandardFilter(false, _storeContext.CurrentStore.Id).ToDictionary(x => x.Id);

                if (customer != null && !customer.IsSearchEngineAccount())
                {
                    // Search engines should always crawl by primary store currency
                    var customerCurrencyId = customer.GenericAttributes.CurrencyId;
                    if (customerCurrencyId > 0)
                    {
                        if (storeCurrenciesMap.TryGetValue(customerCurrencyId.Value, out currency))
                        {
                            currency = VerifyCurrency(currency);
                            if (currency == null)
                            {
                                await SetCustomerCurrencyAsync(null, true);
                            }
                        }
                    }
                }

                // if there's only one currency for current store it dominates the primary currency
                if (storeCurrenciesMap.Count == 1)
                {
                    currency = storeCurrenciesMap[storeCurrenciesMap.Keys.First()];
                }

                // Default currency of country to which the current IP address belongs.
                if (currency == null)
                {
                    var ipAddress = _webHelper.GetClientIpAddress();
                    var lookupCountry = _geoCountryLookup.LookupCountry(ipAddress);
                    if (lookupCountry != null)
                    {
                        var country = await _db.Countries
                            .AsNoTracking()
                            .Include(x => x.DefaultCurrency)
                            .ApplyIsoCodeFilter(lookupCountry.IsoCode)
                            .FirstOrDefaultAsync();

                        if (country?.DefaultCurrency?.Published == true)
                        {
                            currency = country.DefaultCurrency;
                        }
                    }
                }

                // Find currency by domain ending
                if (currency == null)
                {
                    var request = _httpContextAccessor.HttpContext?.Request;
                    if (request != null)
                    {
                        currency = storeCurrenciesMap.Values.GetByDomainEnding(request.Host.Value);
                    }
                }

                // Get default currency.
                if (currency == null)
                {
                    currency = VerifyCurrency(storeCurrenciesMap.Get(_storeContext.CurrentStore.DefaultCurrencyId));
                }

                // Get primary currency.
                if (currency == null)
                {
                    currency = VerifyCurrency(_currencyService.Value.PrimaryCurrency);
                }

                // Get the first published currency for current store
                if (currency == null)
                {
                    currency = storeCurrenciesMap.Values.FirstOrDefault();
                }
            }

            // If not found in currencies filtered by the current store, then return any currency
            if (currency == null)
            {
                currency = await query.ApplyStandardFilter().FirstOrDefaultAsync();
            }

            // No published currency available (fix it)
            if (currency == null)
            {
                currency = await query.AsTracking().ApplyStandardFilter(true).FirstOrDefaultAsync();
                if (currency != null)
                {
                    currency.Published = true;
                    await _db.SaveChangesAsync();
                }
            }

            return currency;
        }

        private static Currency VerifyCurrency(Currency currency)
        {
            if (currency != null && !currency.Published)
            {
                return null;
            }

            return currency;
        }

        private async Task SetCustomerCurrencyAsync(int? currencyId, bool async)
        {
            var customer = CurrentCustomer;
            var customerAttributes = customer.GenericAttributes;

            customerAttributes.CurrencyId = currencyId;

            if (async)
            {
                await customerAttributes.SaveChangesAsync();
            }
            else
            {
                customerAttributes.SaveChanges();
            }
        }

        public TaxDisplayType TaxDisplayType
        {
            get => GetTaxDisplayTypeFor(CurrentCustomer, _storeContext.CurrentStore.Id);
            set
            {
                if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                {
                    CurrentCustomer.TaxDisplayTypeId = (int)value;
                    _db.SaveChanges();
                }

                _taxDisplayTypes[(CurrentCustomer.Id, _storeContext.CurrentStore.Id)] = value;
            }
        }

        public TaxDisplayType GetTaxDisplayTypeFor(Customer customer, int storeId)
        {
            var key = (customer.Id, storeId);

            if (!_taxDisplayTypes.TryGetValue(key, out var result))
            {
                int? taxDisplayTypeId = null;

                if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
                {
                    taxDisplayTypeId = customer.TaxDisplayTypeId;
                }

                if (!taxDisplayTypeId.HasValue && _taxSettings.EuVatEnabled)
                {
                    if (customer != null && _taxService.Value.IsVatExemptAsync(customer).Await())
                    {
                        taxDisplayTypeId = (int)TaxDisplayType.ExcludingTax;
                    }
                }

                if (!taxDisplayTypeId.HasValue)
                {
                    var customerRoles = customer.CustomerRoleMappings.Select(x => x.CustomerRole).ToList();
                    string cacheKey = string.Format(WebCacheInvalidator.CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY, string.Join(",", customerRoles.Select(x => x.Id)), storeId);
                    var cacheResult = _cache.Get(cacheKey, () =>
                    {
                        var roleTaxDisplayTypes = customerRoles
                            .Where(x => x.TaxDisplayType.HasValue)
                            .OrderByDescending(x => x.TaxDisplayType.Value)
                            .Select(x => x.TaxDisplayType.Value);

                        if (roleTaxDisplayTypes.Any())
                        {
                            return (TaxDisplayType)roleTaxDisplayTypes.FirstOrDefault();
                        }

                        return _taxSettings.TaxDisplayType;
                    });

                    taxDisplayTypeId = (int)cacheResult;
                }

                result = (TaxDisplayType)taxDisplayTypeId.Value;
                _taxDisplayTypes[key] = result;
            }

            return result;
        }

        #endregion

        public bool IsAdminArea
        {
            get
            {
                if (_isAdminArea.HasValue)
                {
                    return _isAdminArea.Value;
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _isAdminArea = false;
                }

                if (httpContext.Request.IsAdminArea())
                {
                    _isAdminArea = true;
                }

                // TODO: (core) More checks for admin area?

                _isAdminArea ??= false;
                return _isAdminArea.Value;
            }
            set => _isAdminArea = value;
        }
    }
}
