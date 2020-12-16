using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Content.Seo.Routing
{
    /// <summary>
    /// Builds a <see cref="RouteValueDictionary"/> instance for a given <see cref="UrlRecord"/> entity.
    /// </summary>
    public abstract class SlugRouter
    {
        /// <summary>
        /// Builds a <see cref="RouteValueDictionary"/> instance for a given <see cref="UrlRecord"/> entity.
        /// </summary>
        /// <param name="entity">The matched entity for current slug in request url.</param>
        /// <param name="values">The route values associated with the current match. Implementations should not modify values.</param>
        /// <returns>An instance of <see cref="RouteValueDictionary"/> or <c>null</c> if no route matches.</returns>
        public abstract RouteValueDictionary GetRouteValues(UrlRecord entity, RouteValueDictionary values);

        /// <summary>
        /// Maps routes solely needed for URL creation, NOT for route matching.
        /// This method is called only once per <see cref="SlugRouter"/> instance during application startup.
        /// </summary>
        public abstract void MapRoutes(IEndpointRouteBuilder routes);

        public virtual int Order { get; } = 0;

        protected static string UrlPatternFor(string entityName)
        {
            var url = "{SeName}";
            var prefix = SlugRouteTransformer.GetUrlPrefixFor(entityName);
            if (prefix.HasValue())
            {
                return prefix + "/" + "{SeName}";
            }

            return url;
        }
    }
}