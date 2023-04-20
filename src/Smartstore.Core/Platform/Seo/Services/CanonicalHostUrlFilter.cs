using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo.Routing;
using Smartstore.Utilities;

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

            if (CommonHelper.IsDevEnvironment || httpContext.Connection.IsLocal())
            {
                // Don't attempt to redirect on local host or in dev environment
                return;
            }

            var hasWww = policy.Host.Value.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

            if (rule == CanonicalHostNameRule.OmitWww && hasWww)
            {
                policy.Host.Modify(policy.Host.Value[4..]);
            }
            else if (rule == CanonicalHostNameRule.RequireWww && !hasWww)
            {
                policy.Host.Modify("www." + policy.Host.Value);
            }
        }
    }
}
