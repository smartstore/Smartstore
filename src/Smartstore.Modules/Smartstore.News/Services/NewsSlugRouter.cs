using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.News.Services
{
    public class NewsSlugRouter : SlugRouter
    {
        const string EntityName = nameof(NewsItem);
        
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values, RouteTarget routeTarget = RouteTarget.PublicView)
        {
            if (urlRecord.EntityName.EqualsNoCase(EntityName))
            {
                if (routeTarget == RouteTarget.Edit)
                {
                    return new RouteValueDictionary
                    {
                        { "area", "Admin" },
                        { "controller", "NewsAdmin" },
                        { "action", "Edit" },
                        { "id", urlRecord.EntityId }
                    };
                }
                else
                {
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "News" },
                        { "action", "NewsItem" },
                        { "newsItemId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                }
            }

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapLocalizedControllerRoute("NewsItem", UrlPatternFor(EntityName), new { controller = "News", action = "NewsItem" });
        }
    }
}
