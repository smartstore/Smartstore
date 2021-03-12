using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Seo.Routing
{
    public class DefaultSlugRouter : SlugRouter
    {
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values)
        {
            switch (urlRecord.EntityName.ToLowerInvariant())
            {
                case "product":
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Product" },
                        { "action", "ProductDetails" },
                        { "productId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                case "category":
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Catalog" },
                        { "action", "Category" },
                        { "categoryId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                case "manufacturer":
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Catalog" },
                        { "action", "Manufacturer" },
                        { "manufacturerId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                case "topic":
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Topic" },
                        { "action", "TopicDetails" },
                        { "topicId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                default:
                    //case "newsitem": // TODO: (core) Move newsitem and blogpost to external modules
                    //case "blogpost":
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Home" },
                        { "action", "Slug" },
                        { "entity", urlRecord }
                    };
            }

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            // TODO: (core) check all these SEO routes for correctness once all slug supporting entities are ported.
            routes.MapLocalizedControllerRoute("Product", UrlPatternFor("Product"), new { controller = "Product", action = "ProductDetails" });
            routes.MapLocalizedControllerRoute("Product", UrlPatternFor("Product"), new { controller = "Product", action = "ProductDetails" });
            routes.MapLocalizedControllerRoute("Category", UrlPatternFor("Category"), new { controller = "Catalog", action = "Category" });
            routes.MapLocalizedControllerRoute("Manufacturer", UrlPatternFor("Manufacturer"), new { controller = "Catalog", action = "Manufacturer" });
            routes.MapLocalizedControllerRoute("Topic", UrlPatternFor("Topic"), new { controller = "Topic", action = "TopicDetails" });

            // TODO: (core) Move these slug routes to external modules somehow
            //routes.MapControllerRoute("NewsItem", UrlPatternFor("NewsItem"), new { controller = "News", action = "NewsItem" });
            //routes.MapControllerRoute("BlogPost", UrlPatternFor("BlogPost"), new { controller = "Blog", action = "BlogPost" });
        }
    }
}