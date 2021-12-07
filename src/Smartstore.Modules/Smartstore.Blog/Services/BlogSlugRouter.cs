using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Blog.Services
{
    public class BlogSlugRouter : SlugRouter
    {
        const string EntityName = nameof(BlogPost);

        public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values, RouteTarget routeTarget = RouteTarget.PublicView)
        {
            if (urlRecord.EntityName.EqualsNoCase(EntityName))
            {
                if (routeTarget == RouteTarget.Edit)
                {
                    return new RouteValueDictionary
                    {
                        { "area", "Admin" },
                        { "controller", "BlogAdmin" },
                        { "action", "Edit" },
                        { "id", urlRecord.EntityId }
                    };
                }
                else
                {
                    return new RouteValueDictionary
                    {
                        { "area", string.Empty },
                        { "controller", "Blog" },
                        { "action", "BlogPost" },
                        { "blogPostId", urlRecord.EntityId },
                        { "entity", urlRecord }
                    };
                }
            }

            return null;
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapLocalizedControllerRoute("BlogPost", UrlPatternFor(EntityName), new { controller = "Blog", action = "BlogPost" });
        }
    }
}
