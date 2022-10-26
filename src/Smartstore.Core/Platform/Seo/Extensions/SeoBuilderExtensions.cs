using Microsoft.AspNetCore.Routing;
using Smartstore;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class SeoBuilderExtensions
    {
        const string DefaultDisplayName = "Xml Sitemap";
        const string DefaultPattern = "sitemap.xml/{index:int?}";

        /// <summary>
        /// Determines current URL policy and performs HTTP redirection
        /// if any previous middleware required redirection to a new
        /// valid / sanitized location.
        /// </summary>
        public static IApplicationBuilder UseUrlPolicy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<UrlPolicyMiddleware>();
        }

        /// <summary>
        /// Maps XML sitemap endpoint (sitemap.xml)
        /// </summary>
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
