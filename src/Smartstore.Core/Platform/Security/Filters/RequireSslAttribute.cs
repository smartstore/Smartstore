using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// A marker filter indicating that the current request should be received over HTTPS (if the policies allow this).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RequireSslAttribute : Attribute, IFilterMetadata
    {
    }
}
