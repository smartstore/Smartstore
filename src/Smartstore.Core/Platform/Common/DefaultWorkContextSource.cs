using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Data.Hooks;
using Smartstore.Net;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Core
{
    public class DefaultWorkContextSource : AsyncDbSaveHook<BaseEntity>, IWorkContextSource
    {
        /// <summary>
        /// Key for tax display type caching
        /// </summary>
        /// <remarks>
        /// {0} : customer role ids
        /// {1} : store identifier
        /// </remarks>
        const string CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY = "customerroles:taxdisplaytypes-{0}-{1}";
        const string CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY = "customerroles:taxdisplaytypes*";

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

        public DefaultWorkContextSource(
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

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is CustomerRole)
            {
                await _cache.RemoveByPatternAsync(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY);
                return HookResult.Ok;
            }
            else if (entry.Entity is Setting setting && entry.InitialState == EntityState.Modified)
            {
                if (setting.Name.EqualsNoCase(TypeHelper.NameOf<TaxSettings>(x => x.TaxDisplayType, true)))
                {
                    await _cache.RemoveByPatternAsync(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY); // depends on TaxSettings.TaxDisplayType
                }
                return HookResult.Ok;
            }
            else
            {
                return HookResult.Void;
            }
        }

        public async virtual Task<(Customer, Customer)> ResolveCurrentCustomerAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Is system account?
            if ((await TryGetSystemAccountAsync(httpContext)).Out(out var customer))
            {
                // Get out quickly. Bots tend to overstress the shop.
                return (customer, null);
            }

            // Registered/Authenticated customer?
            customer = await _customerService.GetAuthenticatedCustomerAsync();

            Customer impersonator = null;

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
                        impersonator = customer;
                        customer = impersonatedCustomer;
                    }
                }
            }

            // Load guest customer
            if (customer == null || customer.Deleted || !customer.Active)
            {
                customer = await GetGuestCustomerAsync(httpContext);
            }

            return (customer, impersonator);
        }

        protected virtual async Task<AsyncOut<Customer>> TryGetSystemAccountAsync(HttpContext context)
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

        public async virtual Task<Language> ResolveWorkingLanguageAsync(Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));
            
            // Resolve the current working language
            var language = await _languageResolver.ResolveLanguageAsync(customer, _httpContextAccessor.HttpContext);

            // Set language if current customer langid does not match resolved language id
            var customerAttributes = customer.GenericAttributes;
            if (customerAttributes.LanguageId != language.Id)
            {
                await SaveCustomerAttribute(customer, SystemCustomerAttributeNames.LanguageId, language.Id, true);
            }

            return language;
        }

        public virtual async Task<Currency> ResolveWorkingCurrencyAsync(Customer customer, bool forAdminArea)
        {
            var query = _db.Currencies.AsNoTracking();

            Currency currency = null;

            // Return primary store currency when we're in admin area/mode
            if (forAdminArea)
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
                                await SaveCustomerAttribute(customer, SystemCustomerAttributeNames.CurrencyId, null, true);
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

        public virtual async Task<TaxDisplayType> ResolveTaxDisplayTypeAsync(Customer customer, int storeId)
        {
            int? taxDisplayTypeId = null;

            if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
            {
                taxDisplayTypeId = customer.TaxDisplayTypeId;
            }

            if (taxDisplayTypeId == null && _taxSettings.EuVatEnabled)
            {
                if (customer != null && await _taxService.Value.IsVatExemptAsync(customer))
                {
                    taxDisplayTypeId = (int)TaxDisplayType.ExcludingTax;
                }
            }

            if (taxDisplayTypeId == null)
            {
                var customerRoles = customer.CustomerRoleMappings.Select(x => x.CustomerRole).ToList();
                string cacheKey = string.Format(CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY, string.Join(",", customerRoles.Select(x => x.Id)), storeId);
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

            return (TaxDisplayType)taxDisplayTypeId.Value;
        }

        public Task SaveCustomerAttribute(Customer customer, string name, int? value, bool async)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotEmpty(name, nameof(name));

            if (customer.IsSystemAccount)
            {
                return Task.CompletedTask;
            }

            var attributes = customer.GenericAttributes;
            attributes.Set(name, value, attributes.CurrentStoreId);

            if (async)
            {
                return attributes.SaveChangesAsync();
            }
            else
            {
                attributes.SaveChanges();
                return Task.CompletedTask;
            }
        }
    }
}
