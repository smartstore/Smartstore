using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Applies all configured rules for canonical URLs.
    /// </summary>
    public class CanonicalHostUrlFilter : IUrlFilter
    {
        public void Apply(UrlPolicy policy, HttpContext httpContext)
        {
            var rule = policy.SeoSettings.CanonicalHostNameRule;
            if (rule == CanonicalHostNameRule.NoRule)
            {
                return;
            }

            if (httpContext.Connection.IsLocal())
            {
                // Allows testing of "localtest.me"
                return;
            }

            var hasWww = policy.Host.Value.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

            if (rule == CanonicalHostNameRule.OmitWww && hasWww)
            {
                policy.Host.Modify(policy.Host.Value.Substring(4));
            }
            else if (rule == CanonicalHostNameRule.RequireWww && !hasWww)
            {
                policy.Host.Modify("www." + policy.Host.Value);
            }
        }
    }
}
