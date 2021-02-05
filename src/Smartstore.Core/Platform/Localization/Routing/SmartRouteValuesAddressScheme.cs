using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Seo.Routing;

namespace Smartstore.Core.Localization.Routing
{
    public class SmartRouteValuesAddressScheme : IEndpointAddressScheme<RouteValuesAddress>, IDisposable
    {
        private readonly IEndpointAddressScheme<RouteValuesAddress> _inner;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartRouteValuesAddressScheme(IEndpointAddressScheme<RouteValuesAddress> inner, IHttpContextAccessor httpContextAccessor)
        {
            _inner = inner;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<Endpoint> FindEndpoints(RouteValuesAddress address)
        {
            var endpoints = _inner.FindEndpoints(address);

            if (ShouldTryCultureNeutralEndpoint(address, endpoints))
            {
                // Link generation by route name fails, because we actually create two named routes:
                // 1 = {RouteName}, 2 = {RouteName}__noculture (the latter for code-less default language according to settings).
                // In case the current request is in default culture and "DefaultLanguageRedirectBehaviour.StripSeoCode"
                // is set, we have to refer to {RouteName}__noculture, not {RouteName}, otherwise culture code gets appended as querystring.
                address.RouteName += "__noculture";
                endpoints = _inner.FindEndpoints(address);
            }

            return endpoints;
        }

        private bool ShouldTryCultureNeutralEndpoint(RouteValuesAddress address, IEnumerable<Endpoint> endpoints)
        {
            if (address.RouteName.HasValue() && endpoints.Any())
            {
                var urlPolicy = _httpContextAccessor.HttpContext?.RequestServices?.GetService<UrlPolicy>();
                var localizationSettings = urlPolicy?.LocalizationSettings;
                var explicitCulture = address.ExplicitValues.GetCultureCode();
                var hasExplicitCulture = explicitCulture.HasValue();

                var isDefaultCulture = hasExplicitCulture
                    ? explicitCulture.EqualsNoCase(urlPolicy.DefaultCultureCode)
                    : urlPolicy.IsDefaultCulture;

                if (localizationSettings?.SeoFriendlyUrlsForLanguagesEnabled == true && isDefaultCulture)
                {
                    var localizedRouteMetadata = endpoints.FirstOrDefault()?.Metadata?.OfType<LocalizedRouteMetadata>().FirstOrDefault();
                    if (localizedRouteMetadata != null && !localizedRouteMetadata.IsCultureNeutralRoute)
                    {
                        if (localizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.StripSeoCode)
                        {
                            if (hasExplicitCulture)
                            {
                                // We gonna remove explicit culture value. The code would be appended to URL otherwise,
                                // because we instructed the endpoint matcher to use the culture agnostic route. Culture agnostic
                                // routes do not contain the {culture} prefix.
                                address.ExplicitValues.Remove("culture");
                            }
                            
                            return true;
                        }
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
