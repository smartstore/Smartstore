using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// A marker filter indicating that the endpoint is solely called by webhook clients/callers.
    /// The purpose is to suppress customer detection for such endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class WebhookEndpointAttribute : Attribute, IFilterMetadata
    {
    }
}
