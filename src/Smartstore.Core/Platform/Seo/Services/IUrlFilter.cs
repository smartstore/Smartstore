using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Responsible for applying URL rules to current request url (HTTPS, canonical host name, culture code etc.)
    /// </summary>
    public interface IUrlFilter
    {
        void Apply(UrlPolicy policy, HttpContext httpContext);
    }
}