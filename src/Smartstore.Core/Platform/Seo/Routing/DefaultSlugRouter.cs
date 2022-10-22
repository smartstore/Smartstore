using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Seo.Routing
{
    public class DefaultSlugRouter : SlugRouter
    {
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values, RouteTarget routeTarget = RouteTarget.PublicView)
        {
            if (routeTarget == RouteTarget.Edit)
            {
                return null;
            }

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
            }

            return null;
        }

        public override IEndpointConventionBuilder MapRoutes(IEndpointRouteBuilder routes)
        {
            return routes.MapComposite(new[]
            {
                routes.MapLocalizedControllerRoute("Product", UrlPatternFor("Product"), new { controller = "Product", action = "ProductDetails" }),
                routes.MapLocalizedControllerRoute("Category", UrlPatternFor("Category"), new { controller = "Catalog", action = "Category" }),
                routes.MapLocalizedControllerRoute("Manufacturer", UrlPatternFor("Manufacturer"), new { controller = "Catalog", action = "Manufacturer" }),
                routes.MapLocalizedControllerRoute("Topic", UrlPatternFor("Topic"), new { controller = "Topic", action = "TopicDetails" })
            });
        }
    }
}