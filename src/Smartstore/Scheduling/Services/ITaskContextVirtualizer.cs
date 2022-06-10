using Microsoft.AspNetCore.Http;

namespace Smartstore.Scheduling
{
    /// <summary>
    /// Responsible for virtualizing context data (like current user, current store etc.)
    /// </summary>
    public partial interface ITaskContextVirtualizer
    {
        /// <summary>
        /// Virtualized context data (like current user, current store etc.)
        /// </summary>
        Task VirtualizeAsync(HttpContext httpContext, IDictionary<string, string> taskParameters = null);
    }
}