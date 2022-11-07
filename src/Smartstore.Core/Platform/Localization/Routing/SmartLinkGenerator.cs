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

        public override string GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            pathBase = GetPathBase(_httpContextAccessor.HttpContext, address, pathBase);
            return _inner.GetPathByAddress(httpContext, address, values, ambientValues, pathBase, fragment, options);
        }

        public override string GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            pathBase = GetPathBase(_httpContextAccessor.HttpContext, address, pathBase) ?? new PathString();
            return _inner.GetPathByAddress(address, values, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, string scheme = null, HostString? host = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            pathBase = GetPathBase(_httpContextAccessor.HttpContext, address, pathBase);
            return _inner.GetUriByAddress(httpContext, address, values, ambientValues, scheme, host, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values, string scheme, HostString host, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            pathBase = GetPathBase(_httpContextAccessor.HttpContext, address, pathBase) ?? new PathString();
            return _inner.GetUriByAddress(address, values, scheme, host, pathBase, fragment, options);
        }

        private PathString? GetPathBase<TAddress>(HttpContext httpContext, TAddress address, PathString? pathBase)
        {
            if (httpContext == null)
            {
                return pathBase;
            }

            var urlPolicy = httpContext.GetUrlPolicy();
            if (urlPolicy == null)
            {
                // Policy is null before UseLocalizedRouting middleware
                return pathBase;
            }

            var localizationSettings = urlPolicy.LocalizationSettings;
            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // No need to go further if SEO url generation is turned off.
                return pathBase;
            }

            if (address is not RouteValuesAddress routeValueAddress)
            {
                return pathBase;
            }

            var isLocalizedRouteEndpoint = IsLocalizedRouteEndpoint(routeValueAddress);
            if (!isLocalizedRouteEndpoint)
            {
                // Target is not localized. No need to prepend culture.
                return pathBase;
            }

            var explicitValues = routeValueAddress.ExplicitValues;
            var ambientValues = routeValueAddress.AmbientValues;

            if (ambientValues == null || explicitValues == null)
            {
                return pathBase;
            }

            var shouldStripDefaultCode = localizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.StripSeoCode;

            if (explicitValues.TryGetValueAs<string>("culture", out var explicitCulture))
            {
                // "culture" has been set as explicit route value, e.g. with "a" tag helper > asp-route-culture

                // Remove explicit culture from route values.
                // Otherwise, "culture" will be appended as querystring to the generated URL.
                routeValueAddress.ExplicitValues.Remove("culture");

                return TryAppendToPathBase(explicitCulture);
            }

            if (!ambientValues.TryGetValueAs<string>("culture", out var currentCultureCode))
            {
                // The current request's endpoint route is not localizable ("culture" token would be present otherwise).
                // But link generation must respect LocalizationSettings configuration.

                var workContext = httpContext.RequestServices.GetRequiredService<IWorkContext>();
                currentCultureCode = workContext.IsInitialized 
                    ? workContext.WorkingLanguage.GetTwoLetterISOLanguageName()
                    : urlPolicy.Culture.Value ?? urlPolicy.DefaultCultureCode;
                var isDefaultCulture = currentCultureCode.EqualsNoCase(urlPolicy.DefaultCultureCode);

                if (isDefaultCulture && shouldStripDefaultCode)
                {
                    // Special case where store's default culture matches current (resolved) culture,
                    // but no seo code should be prepended to URLs by configuration. We can stop here.
                    return pathBase;
                }
            }

            if (urlPolicy.IsInvalidUrl && urlPolicy.Culture.HasValue)
            {
                // The CultureRedirectionMiddleware detected a localized URL, but the locale does not exist or is inactive.
                // The routing system is therefore about to render the "NotFound" view. Here we ensure that generated links
                // in NotFound page do not contain the invalid seo code anymore: Either we strip it off or we replace it
                // with the default language's seo code (according to "LocalizationSettings.DefaultLanguageRedirectBehaviour" setting).
                currentCultureCode = urlPolicy.Culture.Value;
            }

            if (shouldStripDefaultCode && currentCultureCode.EqualsNoCase(urlPolicy.DefaultCultureCode))
            {
                return pathBase;
            }
            else
            {
                // Set explicit culture value only when candidate endpoints routes are localizable.
                return TryAppendToPathBase(currentCultureCode);
            }

            PathString TryAppendToPathBase(string culture)
            {
                return pathBase == null
                    ? new PathString('/' + culture)
                    : pathBase.Value.Add(culture);
            }
        }

        private bool IsLocalizedRouteEndpoint(RouteValuesAddress address)
        {
            var addressingScheme = _serviceProvider.GetRequiredService<IEndpointAddressScheme<RouteValuesAddress>>();

            // Find all route endpoint candidates that match the given address
            var endpoints = addressingScheme.FindEndpoints(address).OfType<RouteEndpoint>();

            // Check whether current address contains a non-empty area token (either ambient or explicit)
            var hasArea =
                (address.ExplicitValues.TryGetValueAs(AreaParam, out string area) && area.Length > 0) ||
                (address.AmbientValues.TryGetValueAs(AreaParam, out area) && area.Length > 0);

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
                        IsLocalized = endpoint.Metadata.GetMetadata<ILocalizedRoute>() != null
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

        public void Dispose()
        {
            if (_inner is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}