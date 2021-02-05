using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Seo.Routing;

namespace Smartstore.Core.Localization.Routing
{
    public class SmartLinkGenerator : LinkGenerator, IDisposable
    {
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
                    var addressingScheme = _serviceProvider.GetRequiredService<IEndpointAddressScheme<TAddress>>();
                    var endpoints = addressingScheme.FindEndpoints(address).OfType<RouteEndpoint>();
                    var isLocalizedRouteEndpoint = endpoints.SelectMany(x => x.Metadata).OfType<LocalizedRouteMetadata>().Any();

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