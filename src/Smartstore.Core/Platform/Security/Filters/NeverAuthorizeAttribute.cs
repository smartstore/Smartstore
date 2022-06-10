using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// A marker filter indicating that the access to the requested endpoint is always permitted (no permission check).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NeverAuthorizeAttribute : Attribute, IFilterMetadata
    {
    }
}
