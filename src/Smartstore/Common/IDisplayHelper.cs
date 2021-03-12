using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore
{
    /// <summary>
    /// Global helper for display, rendering and templating
    /// </summary>
    public partial interface IDisplayHelper
    {
        HttpContext HttpContext { get; }
    }

    public static class IDisplayHelperExtensions
    {
        /// <summary>
        /// Resolves a service from scoped service container.
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Resolve<T>(this IDisplayHelper displayHelper) where T : notnull
        {
            return displayHelper.HttpContext.RequestServices.GetService<T>();
        }
    }

    public partial class DefaultDisplayHelper : IDisplayHelper
    {
        public DefaultDisplayHelper(IHttpContextAccessor httpContextAccessor)
        {
            HttpContext = httpContextAccessor.HttpContext;
        }

        public HttpContext HttpContext { get; }
    }
}
