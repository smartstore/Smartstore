using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Content.Seo.Routing
{
    public class DefaultSlugRouter : SlugRouter
    {
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values)
        {
            // TODO: (core) Implement DefaultSlugRouter once all affected entity types are ported.
            switch (urlRecord.EntityName.ToLowerInvariant())
            {
                case "product":
                case "category":
                case "manufacturer":
                case "topic":
                case "newsitem":
                case "blogpost":
                    return new RouteValueDictionary
                    {
                        { "area", "" },
                        { "controller", "Home" },
                        { "action", "Slug" },
                        { "entity", urlRecord }
                    };
                default:
                    break;
            }

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            //routes.MapLocalizedControllerRoute("Product", UrlPatternFor("Product"), new { controller = "Product", action = "ProductDetails" });

            // TODO: (core) check all these SEO routes for correctness once all slug supporting entities are ported.
            routes.MapControllerRoute("Product", UrlPatternFor("Product"), new { controller = "Product", action = "ProductDetails" });
            routes.MapControllerRoute("Category", UrlPatternFor("Category"), new { controller = "Catalog", action = "Category" });
            routes.MapControllerRoute("Manufacturer", UrlPatternFor("Manufacturer"), new { controller = "Catalog", action = "Manufacturer" });
            routes.MapControllerRoute("Topic", UrlPatternFor("Topic"), new { controller = "Topic", action = "TopicDetails" });
            routes.MapControllerRoute("NewsItem", UrlPatternFor("NewsItem"), new { controller = "News", action = "NewsItem" });
            routes.MapControllerRoute("BlogPost", UrlPatternFor("BlogPost"), new { controller = "Blog", action = "BlogPost" });
        }
    }
}