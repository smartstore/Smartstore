using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Localization.Routing
{
    public interface ILocalizedRoute
    {
    }

    public sealed class LocalizedRouteMetadata : ILocalizedRoute
    {
    }

    /// <summary>
    /// Specifies a localized attribute route on a controller action method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class LocalizedRouteAttribute : RouteAttribute, ILocalizedRoute
    {
        /// <summary>
        /// Creates a new <see cref="LocalizedRouteAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public LocalizedRouteAttribute(string template)
            : base(template)
        {
        }
    }
}
