using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Localization.Routing
{
    public class SmartLinkGenerator : LinkGenerator, IDisposable
    {
        protected struct EndpointInfo
        {
            public bool HasArea { get; set; }
            public bool IsLocalized { get; set; }
        }

        private readonly static ConcurrentDictionary<string, EndpointInfo> _endpointInfoCache = new();
        
        const string AreaParam = "area";

        private readonly LinkGenerator _inner;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartLinkGenerator(LinkGenerator inner, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
        {
            _inner = inner;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        private void TryCopyCultureTokenToExplicitValues<TAddress>(HttpContext httpContext, TAddress address)
        {
            if (httpContext == null)
            {
                return;
            }

            if (address is RouteValuesAddress routeValueAddress && routeValueAddress.AmbientValues != null && routeValueAddress.ExplicitValues != null)
            {
                if (routeValueAddress.ExplicitValues.ContainsKey("culture"))
                {
                    // Get out, 'cause "culture" has been set as explicit route value, e.g. with "a" tag helper > asp-route-culture
                    return;
                }

                var services = httpContext.RequestServices;
                var urlPolicy = services.GetRequiredService<UrlPolicy>();

                if (!routeValueAddress.AmbientValues.TryGetValue("culture", out var currentCultureCode))
                {
                    // The current request's endpoint route is not localizable (culture token would be present otherwise).
                    // But link generation must respect LocalizationSettings configuration.

                    var localizationSettings = urlPolicy.LocalizationSettings;
                    if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                    {
                        // No need to go further if seo url generation is turned off.
                        return;
                    }

                    var workingLanguage = urlPolicy.WorkingLanguage;
                    if (workingLanguage != null)
                    {
                        currentCultureCode = workingLanguage.GetTwoLetterISOLanguageName();

                        if (urlPolicy.IsDefaultCulture && localizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.StripSeoCode)
                        {
                            // Very special case where store's default culture matches current (resolved) culture,
                            // but no seo code should be prepended to URLs by configuration. We can stop here.

                            // TODO: (core) looks like in this case the culture token is appended to the URL as a query string.
                            return;
                        }
                    }
                }

                if (currentCultureCode != null)
                {
                    var isLocalizedRouteEndpoint = IsLocalizedRouteEndpoint(routeValueAddress);

                    if (isLocalizedRouteEndpoint)
                    {
                        if (urlPolicy.IsInvalidUrl && urlPolicy.Culture.HasValue)
                        {
                            // The CultureRedirectionMiddleware detected a localized URL, but the locale does not exist or is inactive.
                            // The routing system is therefore about to render the "NotFound" view. Here we ensure that generated links
                            // in NotFound page do not contain the invalid seo code anymore: Either we strip it off or we replace it
                            // with the default language's seo code (according to "LocalizationSettings.DefaultLanguageRedirectBehaviour" setting).
                            currentCultureCode = urlPolicy.Culture.Value;
                        }

                        // Set explicit culture value only when candidate endpoints routes are localizable,
                        // otherwise culture will be appended as querystring (?culture=en).
                        routeValueAddress.ExplicitValues["culture"] = currentCultureCode;
                    }
                }
            }
        }

        private bool IsLocalizedRouteEndpoint(RouteValuesAddress address)
        {
            var addressingScheme = _serviceProvider.GetRequiredService<IEndpointAddressScheme<RouteValuesAddress>>();

            // Find all route endpoint candidates that match the given address
            var endpoints = addressingScheme.FindEndpoints(address).OfType<RouteEndpoint>();

            // Check whether current address contains a non-empty area token (either ambient or explicit)
            var hasArea = address.AmbientValues.TryGetValue(AreaParam, out var area) || address.ExplicitValues.TryGetValue(AreaParam, out area);
            hasArea = hasArea && area is string str && !string.IsNullOrEmpty(str);

            // Reduce the set of candidate endpoints by evaluating area:
            // Both current address and candidate endpoint must contain or NOT contain area.
            foreach (var endpoint in endpoints)
            {
                var endpointInfo = _endpointInfoCache.GetOrAdd(endpoint.RoutePattern.RawText, key => 
                {
                    var pattern = endpoint.RoutePattern;
                    var endpointContainsArea = pattern.Defaults.ContainsKey(AreaParam);

                    if (!endpointContainsArea)
                    {
                        for (var i = 0; i < pattern.Parameters.Count; i++)
                        {
                            if (pattern.Parameters[i].Name == AreaParam)
                            {
                                endpointContainsArea = true;
                                break;
                            }
                        }
                    }

                    return new EndpointInfo 
                    { 
                        HasArea = endpointContainsArea, 
                        IsLocalized = endpoint.Metadata.OfType<LocalizedRouteMetadata>().Any()
                    };
                });

                if (hasArea == endpointInfo.HasArea)
                {
                    if (endpointInfo.IsLocalized)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override string GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(httpContext, address);
            return _inner.GetPathByAddress(httpContext, address, values, ambientValues, pathBase, fragment, options);
        }

        public override string GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(_httpContextAccessor.HttpContext, address);
            return _inner.GetPathByAddress(address, values, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, string scheme = null, HostString? host = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(httpContext, address);
            return _inner.GetUriByAddress(httpContext, address, values, ambientValues, scheme, host, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values, string scheme, HostString host, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(_httpContextAccessor.HttpContext, address);
            return _inner.GetUriByAddress(address, values, scheme, host, pathBase, fragment, options);
        }

        public void Dispose()
        {
            if (_inner is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}