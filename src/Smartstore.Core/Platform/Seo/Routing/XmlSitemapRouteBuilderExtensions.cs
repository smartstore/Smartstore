using System;
using Microsoft.AspNetCore.Routing;
using Smartstore;
using Smartstore.Core.Seo;

namespace Microsoft.AspNetCore.Builder
{
    public static class XmlSitemapRouteBuilderExtensions
    {
        const string DefaultDisplayName = "Xml Sitemap";
        const string DefaultPattern = "sitemap.xml/{index:int?}";

        public static IEndpointConventionBuilder MapXmlSitemap(this IEndpointRouteBuilder endpoints)
        {
            Guard.NotNull(endpoints, nameof(endpoints));

            var pipeline = endpoints.CreateApplicationBuilder()
               .UseMiddleware<XmlSitemapMiddleware>()
               .Build();

            return endpoints.MapLocalized(DefaultPattern, pipeline)
                .WithDisplayName(DefaultDisplayName);
        }
    }
}
