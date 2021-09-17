using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Services
{
    // TODO: (mg) (core) verify ForumSlugRouter when forum public controller is ready.
    public class ForumSlugRouter : SlugRouter
    {
        private static readonly string[] _entitiesWithSlugs = new[] { nameof(ForumGroup), nameof(ForumTopic), nameof(Forum) };

        public override RouteValueDictionary GetRouteValues(UrlRecord entity, RouteValueDictionary values)
        {
            return entity.EntityName.EmptyNull().ToLower() switch
            {
                "forumgroup" => GetRouteValues(nameof(ForumGroup)),
                "forumtopic" => GetRouteValues(nameof(ForumTopic)),
                "forum" => GetRouteValues(nameof(Forum)),
                _ => null,
            };

            RouteValueDictionary GetRouteValues(string action)
            {
                return new RouteValueDictionary
                {
                    { "area", string.Empty },
                    { "controller", "Forum" },
                    { "action", action },
                    { "id", entity.EntityId },
                    { "entity", entity }
                };
            }
        }

        public override void MapRoutes(IEndpointRouteBuilder routes)
        {
            foreach (var entity in new[] { nameof(ForumGroup), nameof(ForumTopic), nameof(Forum) })
            {
                routes.MapLocalizedControllerRoute(entity, UrlPatternFor(entity), new { controller = "forum", action = entity, area = string.Empty });
            }
        }
    }
}
