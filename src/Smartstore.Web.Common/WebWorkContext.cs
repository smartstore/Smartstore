using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using System.Threading.Tasks;
using Smartstore.Net;

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
        private readonly TaxSettings _taxSettings;
        private readonly ICacheManager _cache;
        private readonly IUserAgent _userAgent;
        private readonly IWebHelper _webHelper;
        private readonly IGeoCountryLookup _geoCountryLookup;

        private TaxDisplayType? _taxDisplayType;
        private Language _language;
        private Customer _customer;
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
            _taxSettings = taxSettings;
            _taxService = taxService;
            _currencyService = currencyService;
            _userAgent = userAgent;
            _cache = cache;
            _webHelper = webHelper;
            _geoCountryLookup = geoCountryLookup;
        }

        public Customer CurrentCustomer 
        {
            get 
            {
                if (_customer != null)
                {
                    return _customer;
                }

                var httpContext = _httpContextAccessor.HttpContext;

                // Is system account?
                if (TryGetSystemAccount(httpContext, out var customer))
                {
                    // Get out quickly. Bots tend to overstress the shop.
                    _customer = customer;
                    return customer;
                }

                // Registered/Authenticated customer?
                customer = _customerService.GetAuthenticatedCustomerAsync().Await();

                // impersonate user if required (currently used for 'phone order' support)
                if (customer != null)
                {
                    var impersonatedCustomerId = customer.GenericAttributes.ImpersonatedCustomerId;
                    if (impersonatedCustomerId > 0)
                    {
                        var impersonatedCustomer = _db.Customers
                            .IncludeCustomerRoles()
                            .FindById(impersonatedCustomerId.Value);

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
                    customer = GetGuestCustomerAsync(httpContext).Await();
                }

                _customer = customer;

                return _customer;
            }
            set => _customer = value; 
        }

        protected bool TryGetSystemAccount(HttpContext context, out Customer customer)
        {
            // Never check whether customer is deleted/inactive in this method.
            // System accounts should neither be deletable nor activatable, they are mandatory.

            customer = null;

            // check whether request is made by a background task
            // in this case return built-in customer record for background task
            if (context != null && context.Request.IsCalledByTaskScheduler())
            {
                customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
            }

            // check whether request is made by a search engine
            // in this case return built-in customer record for search engines 
            if (customer == null && _userAgent.IsBot)
            {
                customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
            }

            // check whether request is made by the PDF converter
            // in this case return built-in customer record for the converter
            if (customer == null && _userAgent.IsPdfConverter)
            {
                customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.PdfConverter);
            }

            return customer != null;
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
                    IsEssential = true
                };

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

        public Language WorkingLanguage 
        {
            get
            {
                if (_language == null)
                {
                    var customer = CurrentCustomer;

                    // Resolve the current working language
                    _language = _languageResolver.ResolveLanguageAsync(customer, _httpContextAccessor.HttpContext).Await();

                    // Set language if current customer langid does not match resolved language id
                    var customerAttributes = customer.GenericAttributes;
                    if (customerAttributes.LanguageId != _language.Id)
                    {
                        SetCustomerLanguage(_language.Id);
                    }
                }

                return _language;
            }
            set
            {
                if (value?.Id != _language?.Id)
                {
                    SetCustomerLanguage(value?.Id);
                    _language = null;
                }
            }
        }

        private void SetCustomerLanguage(int? languageId)
        {
            var customer = CurrentCustomer;

            if (customer == null || customer.IsSystemAccount)
                return;

            customer.GenericAttributes.LanguageId = languageId;
            customer.GenericAttributes.SaveChanges();
        }

        public Currency WorkingCurrency
        {
            get
            {
                if (_currency != null)
                {
                    return _currency;
                }

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
                    var customer = CurrentCustomer;
                    var storeCurrenciesMap = query.ApplyStandardFilter(storeId: _storeContext.CurrentStore.Id).ToDictionary(x => x.Id);

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
                                    SetCustomerCurrency(null);
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
                            var country = _db.Countries
                                .AsNoTracking()
                                .Include(x => x.DefaultCurrency)
                                .ApplyIsoCodeFilter(lookupCountry.IsoCode)
                                .FirstOrDefault();

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

                    // Get PrimaryStoreCurrency
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
                    currency = query.ApplyStandardFilter().FirstOrDefault();

                }

                // No published currency available (fix it)
                if (currency == null)
                {
                    currency = query.AsTracking().ApplyStandardFilter(true).FirstOrDefault();
                    if (currency != null)
                    {
                        currency.Published = true;
                        _db.SaveChanges();
                    }
                }

                _currency = currency;
                return _currency;
            }
            set 
            {
                if (value?.Id != _currency?.Id)
                {
                    SetCustomerCurrency(value?.Id);
                    _currency = null;
                }
            }
        }

        private static Currency VerifyCurrency(Currency currency)
        {
            if (currency != null && !currency.Published)
            {
                return null;
            }

            return currency;
        }

        private void SetCustomerCurrency(int? currencyId)
        {
            var customer = CurrentCustomer;
            customer.GenericAttributes.CurrencyId = currencyId;
            customer.GenericAttributes.SaveChanges();
        }

        public TaxDisplayType TaxDisplayType 
        {
            get => GetTaxDisplayTypeFor(CurrentCustomer, _storeContext.CurrentStore.Id);
            set => _taxDisplayType = value;
        }

        public TaxDisplayType GetTaxDisplayTypeFor(Customer customer, int storeId)
        {
            if (_taxDisplayType.HasValue)
            {
                return _taxDisplayType.Value;
            }

            int? taxDisplayType = null;

            if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
            {
                taxDisplayType = customer.TaxDisplayTypeId;
            }

            if (!taxDisplayType.HasValue && _taxSettings.EuVatEnabled)
            {
                if (customer != null &&  _taxService.Value.IsVatExemptAsync(customer).Await())
                {
                    taxDisplayType = (int)TaxDisplayType.ExcludingTax;
                }
            }
            
            if (!taxDisplayType.HasValue)
            {
                var customerRoles = customer.CustomerRoleMappings.Select(x => x.CustomerRole).ToList();
                string key = string.Format(WebCacheInvalidator.CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY, string.Join(",", customerRoles.Select(x => x.Id)), storeId);
                var cacheResult = _cache.Get(key, () =>
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

                taxDisplayType = (int)cacheResult;
            }

            _taxDisplayType = (TaxDisplayType)taxDisplayType.Value;
            return _taxDisplayType.Value;
        }

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
