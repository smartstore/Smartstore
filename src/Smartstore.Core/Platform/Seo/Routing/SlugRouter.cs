using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Seo.Routing
{
    /// <summary>
    /// The target (action) of the route.
    /// </summary>
    public enum RouteTarget
    {
        /// <summary>
        /// Target is the public view in frontend.
        /// </summary>
        PublicView,

        /// <summary>
        /// Target is the edit page in backend.
        /// </summary>
        Edit
    }

    /// <summary>
    /// Builds a <see cref="RouteValueDictionary"/> instance for a given <see cref="UrlRecord"/> entity.
    /// </summary>
    public abstract class SlugRouter
    {
        const string CatchAllToken = "{**SeName}";

        /// <summary>
        /// Builds a <see cref="RouteValueDictionary"/> instance for a given <see cref="UrlRecord"/> entity.
        /// </summary>
        /// <param name="entity">The matched entity for current slug in request url.</param>
        /// <param name="values">The route values associated with the current match. Implementations should not modify values.</param>
        /// <returns>An instance of <see cref="RouteValueDictionary"/> or <c>null</c> if no route matches.</returns>
        public abstract RouteValueDictionary GetRouteValues(UrlRecord entity, RouteValueDictionary values, RouteTarget routeTarget = RouteTarget.PublicView);

        /// <summary>
        /// Maps routes solely needed for URL creation, NOT for route matching.
        /// This method is called only once per <see cref="SlugRouter"/> instance during application startup.
        /// </summary>
        public abstract IEndpointConventionBuilder MapRoutes(IEndpointRouteBuilder routes);

        public virtual int Order { get; } = 0;

        protected static string UrlPatternFor(string entityName)
        {
            var prefix = SlugRouteTransformer.GetUrlPrefixFor(entityName);
            if (prefix.HasValue())
            {
                return prefix + '/' + CatchAllToken;
            }
            else
            {
                return CatchAllToken;
            }
        }
    }
}