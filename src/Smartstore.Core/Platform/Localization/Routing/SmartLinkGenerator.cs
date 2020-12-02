using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Localization.Routing
{
    public class SmartLinkGenerator : LinkGenerator
    {
        private readonly LinkGenerator _inner;
        private readonly IServiceProvider _serviceProvider;

        public SmartLinkGenerator(LinkGenerator inner, IServiceProvider serviceProvider)
        {
            _inner = inner;
            _serviceProvider = serviceProvider;
        }

        private void TryCopyCultureTokenToExplicitValues<TAddress>(TAddress address)
        {
            if (address is RouteValuesAddress routeValueAddress && routeValueAddress.AmbientValues != null && routeValueAddress.ExplicitValues != null)
            {
                if (routeValueAddress.AmbientValues.TryGetValue("culture", out var culture) && !routeValueAddress.ExplicitValues.ContainsKey("culture"))
                {
                    var addressingScheme = _serviceProvider.GetRequiredService<IEndpointAddressScheme<TAddress>>();
                    var endpoints = addressingScheme.FindEndpoints(address).OfType<RouteEndpoint>();
                    var isLocalizedRouteEndpoint = endpoints.SelectMany(x => x.Metadata).OfType<LocalizedRouteMetadata>().Any();

                    if (isLocalizedRouteEndpoint)
                    {
                        // Set explicit culture value only when candidate endpoints routes are localizable,
                        // otherwise culture will be appended as querystring (?culture=en).
                        routeValueAddress.ExplicitValues["culture"] = culture;
                    }
                }
            }
        }

        public override string GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(address);
            return _inner.GetPathByAddress(httpContext, address, values, ambientValues, pathBase, fragment, options);
        }

        public override string GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(address);
            return _inner.GetPathByAddress(address, values, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, string scheme = null, HostString? host = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(address);
            return _inner.GetUriByAddress(httpContext, address, values, ambientValues, scheme, host, pathBase, fragment, options);
        }

        public override string GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values, string scheme, HostString host, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            TryCopyCultureTokenToExplicitValues(address);
            return _inner.GetUriByAddress(address, values, scheme, host, pathBase, fragment, options);
        }
    }
}