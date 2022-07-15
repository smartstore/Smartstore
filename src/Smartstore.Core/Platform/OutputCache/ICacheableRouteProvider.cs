using System.Diagnostics;
using Smartstore.Utilities;

namespace Smartstore.Core.OutputCache
{
    /// <summary>
    /// Represents a route to a resource (page or component) that can be cached by output cache.
    /// </summary>
    [DebuggerDisplay("Route: {Route}, Duration: {Duration}")]
    public class CacheableRoute : IEquatable<CacheableRoute>
    {
        public CacheableRoute(string route)
        {
            Guard.NotEmpty(route, nameof(route));
            Route = route;
        }

        /// <summary>
        /// The route identifier. Can be a full page or a view component route.
        /// <list type="bullet">
        ///     <item>
        ///         Full page route pattern: <c>[{Module}/]{ControllerShortName}/{Action}</c>. Module must be omitted
        ///         if controller is part of the application core.
        ///         Example: <c>Smartstore.Blog/Blog/List</c>, <c>Catalog/Category</c>
        ///     </item>
        ///     <item>
        ///         View component route pattern: <c>vc:[{Module}/]{ComponentShortName}</c>. Module must be omitted
        ///         if component is part of the application core.
        ///         Example: <c>vc:SearchBox</c>, <c>vc:Smartstore.Blog/BlogSummary</c>
        ///     </item>
        /// </list>
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Number of seconds the page should be kept in cache on the server.
        /// Only applies to pages, not view components. Set <c>null</c> to fall back to
        /// default duration as specified by the output cache global settings (usually 5 minutes).
        /// </summary>
        public int? Duration { get; init; }

        public int? Tolerance { get; init; }

        public bool IsEnabled { get; init; } = true;

        public bool IsUserRoute { get; init; }

        public bool IsComponentRoute()
        {
            return Route.StartsWith("vc:");
        }

        public override bool Equals(object other)
        {
            return Equals(other as CacheableRoute);
        }

        public bool Equals(CacheableRoute other)
        {
            if (other == null)
                return false;

            return string.Equals(Route, other.Route, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start().Add(typeof(CacheableRoute)).Add(Route).CombinedHash;
        }
    }

    /// <summary>
    /// Provides routes to resources that can be cached by output cache.
    /// </summary>
    public interface ICacheableRouteProvider
    {
        int Order { get; }
        IEnumerable<string> GetCacheableRoutes();
    }
}
