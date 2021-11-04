using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.News.Domain;

namespace Smartstore.News.Services
{
    public class NewsSlugRouter : SlugRouter
    {
        const string EntityName = nameof(NewsItem);
        
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values, bool returnAdminEditRoute = false)
        {
            if (urlRecord.EntityName.EqualsNoCase(EntityName))
            {
                if (returnAdminEditRoute)
                {
                    return new RouteValueDictionary
                    {
                        { "area", "Admin" },
                        { "controller", "News" },
                        { "action", "Edit" },
                        { "id", urlRecord.EntityId }
                    };
                }

                return new RouteValueDictionary
                {
                    { "area", string.Empty },
                    { "controller", "News" },
                    { "action", "NewsItem" },
                    { "newsItemId", urlRecord.EntityId },
                    { "entity", urlRecord }
                };
            }

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapLocalizedControllerRoute("NewsItem", UrlPatternFor(EntityName), new { controller = "News", action = "NewsItem" });
        }
    }
}
