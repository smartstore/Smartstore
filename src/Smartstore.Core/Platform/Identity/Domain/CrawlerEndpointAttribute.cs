using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// A marker filter indicating that the endpoint is most likely called by crawlers/bots.
    /// The purpose is to suppress customer detection for such endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class CrawlerEndpointAttribute : Attribute, IFilterMetadata
    {
    }
}
