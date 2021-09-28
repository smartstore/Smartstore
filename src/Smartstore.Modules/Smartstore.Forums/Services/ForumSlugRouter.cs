using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Services
{
    public class ForumSlugRouter : SlugRouter
    {
        private static readonly string[] _entitiesWithSlugs = new[] { nameof(ForumGroup), nameof(ForumTopic), nameof(Forum) };

        public override RouteValueDictionary GetRouteValues(UrlRecord entity, RouteValueDictionary values)
        {
            return entity.EntityName.EmptyNull().ToLower() switch
            {
                "forumgroup" => GetRouteValues("ForumGroup"),
                "forumtopic" => GetRouteValues("Topic"),
                "forum" => GetRouteValues("Forum"),
                _ => null,
            };

            RouteValueDictionary GetRouteValues(string action)
            {
                return new RouteValueDictionary
                {
                    { "area", string.Empty },
                    { "controller", "Boards" },
                    { "action", action },
                    { "id", entity.EntityId },
                    { "entity", entity }
                };
            }
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapLocalizedControllerRoute(nameof(ForumGroup), UrlPatternFor(nameof(ForumGroup)), new { controller = "Boards", action = "ForumGroup", area = string.Empty });
            routes.MapLocalizedControllerRoute(nameof(ForumTopic), UrlPatternFor(nameof(ForumTopic)), new { controller = "Boards", action = "Topic", area = string.Empty });
            routes.MapLocalizedControllerRoute(nameof(Forum), UrlPatternFor(nameof(Forum)), new { controller = "Boards", action = "Forum", area = string.Empty });
        }
    }
}
