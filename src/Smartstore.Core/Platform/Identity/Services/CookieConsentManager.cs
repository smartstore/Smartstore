using System.Net;
using Autofac;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Smartstore.Caching;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Net;

namespace Smartstore.Core.Identity
{
    public partial class CookieConsentManager : ICookieConsentManager
    {
        private readonly static object _lock = new();
        private static IList<Type> _cookiePublisherTypes = null;

        // {0} = CustomerId, {1} = StoreId
        const string CookieConsentKey = "consent:{0}-{1}";

        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly ITypeScanner _typeScanner;
        private readonly PrivacySettings _privacySettings;
        private readonly IComponentContext _componentContext;
        private readonly IGeoCountryLookup _countryLookup;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IRequestCache _requestCache;

        private bool? _isCookieConsentRequired;

        public CookieConsentManager(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper,
            ITypeScanner typeScanner,
            PrivacySettings privacySettings,
            IComponentContext componentContext,
            IGeoCountryLookup countryLookup,
            IStoreContext storeContext,
            IWorkContext workContext,
            IRequestCache requestCache)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;
            _typeScanner = typeScanner;
            _privacySettings = privacySettings;
            _componentContext = componentContext;
            _countryLookup = countryLookup;
            _storeContext = storeContext;
            _workContext = workContext;
            _requestCache = requestCache;
        }

        public async Task<bool> IsCookieConsentRequiredAsync()
        {
            return _isCookieConsentRequired ??= await IsCookieConsentRequiredCoreAsync(_webHelper.GetClientIpAddress());
        }

        protected virtual async Task<bool> IsCookieConsentRequiredCoreAsync(IPAddress ipAddress)
        {
            if (_privacySettings.CookieConsentRequirement == CookieConsentRequirement.NeverRequired)
            {
                return false;
            }
            else 
            {
                var geoCountry = _countryLookup.LookupCountry(ipAddress);
                if (geoCountry != null)
                {
                    if (_privacySettings.CookieConsentRequirement == CookieConsentRequirement.DependsOnCountry)
                    {           
                        var country = await _db.Countries
                            .AsNoTracking()
                            .ApplyIsoCodeFilter(geoCountry.IsoCode)
                            .FirstOrDefaultAsync();

                        if (country != null && !country.DisplayCookieManager)
                        {
                            return true;
                        }
                    }
                    else if (_privacySettings.CookieConsentRequirement == CookieConsentRequirement.RequiredInEUCountriesOnly)
                    {
                        return geoCountry.IsInEu; 
                    }
                }
            }
            
            return true;
        }

        public virtual async Task<IList<CookieInfo>> GetCookieInfosAsync(bool withUserCookies = false)
        {
            var result = new List<CookieInfo>();
            var publishers = GetAllCookiePublishers();

            foreach (var publisher in publishers)
            {
                var cookieInfo = await publisher.GetCookieInfosAsync();
                if (cookieInfo != null)
                {
                    result.AddRange(cookieInfo);
                }
            }

            // Add user defined cookies from privacy settings.
            if (withUserCookies)
            {
                result.AddRange(GetUserCookieInfos(true));
            }

            return result;
        }

        public virtual IReadOnlyList<CookieInfo> GetUserCookieInfos(bool translated = true)
        {
            if (_privacySettings.CookieInfos.HasValue())
            {
                var cookieInfos = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);

                if (cookieInfos?.Any() ?? false)
                {
                    if (translated)
                    {
                        foreach (var info in cookieInfos)
                        {
                            info.Name = info.GetLocalized(x => x.Name);
                            info.Description = info.GetLocalized(x => x.Description);
                        }
                    }

                    return cookieInfos;
                }
            }

            return new List<CookieInfo>();
        }

        public virtual async Task<bool> IsCookieAllowedAsync(CookieType cookieType)
        {
            Guard.NotNull(cookieType);
            
            if (!await IsCookieConsentRequiredAsync())
            {
                return true;
            }

            var cacheKey = CookieConsentKey.FormatInvariant(_workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id);

            var consentCookie = _requestCache.Get(cacheKey, () =>
            {
                var request = _httpContextAccessor?.HttpContext?.Request;
                if (request != null && request.Cookies.TryGetValue(CookieNames.CookieConsent, out var value) && value.HasValue())
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<ConsentCookie>(value);
                    }
                    catch
                    {
                        // Let's be tolerant in case of error.
                        return new ConsentCookie {
                            AllowAnalytics = true,
                            AllowThirdParty = true,
                            AdPersonalizationConsent = true,
                            AdUserDataConsent = true
                        };
                    }
                }

                return null;
            });

            if (consentCookie != null)
            {
                // Initialise allowedTypes with the required value, as this is always permitted.
                CookieType allowedTypes = CookieType.Required;

                if (consentCookie.AllowAnalytics) allowedTypes |= CookieType.Analytics;
                if (consentCookie.AllowThirdParty) allowedTypes |= CookieType.ThirdParty;
                if (consentCookie.AdUserDataConsent) allowedTypes |= CookieType.ConsentAdUserData;
                if (consentCookie.AdPersonalizationConsent) allowedTypes |= CookieType.ConsentAdPersonalization;

                return (allowedTypes & cookieType) == cookieType;
            }
            
            // If no cookie was set return false.
            return false;
        }

        public virtual ConsentCookie GetCookieData()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                var cookieName = CookieNames.CookieConsent;

                if (context.Items.TryGetValue(cookieName, out var obj))
                {
                    return obj as ConsentCookie;
                }

                if (context.Request?.Cookies?.TryGetValue(cookieName, out string value) ?? false)
                {
                    try
                    {
                        var cookieData = JsonConvert.DeserializeObject<ConsentCookie>(value);
                        context.Items[cookieName] = cookieData;

                        return cookieData;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        public virtual void SetConsentCookie(
            bool allowAnalytics = false, 
            bool allowThirdParty = false,
            bool adUserDataConsent = false,
            bool adPersonalizationConsent = false)
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                var cookieData = new ConsentCookie
                {
                    AllowAnalytics = allowAnalytics,
                    AllowThirdParty = allowThirdParty,
                    AdUserDataConsent = adUserDataConsent,
                    AdPersonalizationConsent = adPersonalizationConsent
                };

                var cookies = context.Response.Cookies;
                var cookieName = CookieNames.CookieConsent;

                var options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(365),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = _webHelper.IsCurrentConnectionSecured()
                };

                cookies.Delete(cookieName, options);
                cookies.Append(cookieName, JsonConvert.SerializeObject(cookieData), options);

                var cacheKey = CookieConsentKey.FormatInvariant(_workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id);

                _requestCache.Remove(cacheKey);
            }
        }

        protected virtual IEnumerable<ICookiePublisher> GetAllCookiePublishers()
        {
            if (_cookiePublisherTypes == null)
            {
                lock (_lock)
                {
                    if (_cookiePublisherTypes == null)
                    {
                        _cookiePublisherTypes = _typeScanner.FindTypes<ICookiePublisher>().ToList();
                    }
                }
            }

            var cookiePublishers = _cookiePublisherTypes
                .Select(type =>
                {
                    if (_componentContext.IsRegistered(type) && _componentContext.TryResolve(type, out var publisher))
                    {
                        return (ICookiePublisher)publisher;
                    }

                    return _componentContext.ResolveUnregistered(type) as ICookiePublisher;
                })
                .ToArray();

            return cookiePublishers;
        }
    }

    /// <summary>
    /// Infos that will be serialized and stored as string in a cookie.
    /// </summary>
    public class ConsentCookie
    {
        /// <summary>
        /// A value indicating whether analytical cookies are allowed to be set.
        /// </summary>
        public bool AllowAnalytics { get; set; }

        /// <summary>
        /// A value indicating whether third party cookies are allowed to be set.
        /// </summary>
        public bool AllowThirdParty { get; set; }

        /// <summary>
        /// A value indicating whether sending of user data is allowed.
        /// </summary>
        public bool AdUserDataConsent { get; set; }

        /// <summary>
        /// A value indicating whether personalization is allowed.
        /// </summary>
        public bool AdPersonalizationConsent { get; set; }
    }
}
