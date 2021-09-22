using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.News.Services
{
    public class NewsSlugRouter : SlugRouter
    {
        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values)
        {
            if (urlRecord.EntityName.ToLowerInvariant() == "newsitem")
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

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapLocalizedControllerRoute("NewsItem", UrlPatternFor("NewsItem"), new { controller = "News", action = "NewsItem" });
        }
    }
}
