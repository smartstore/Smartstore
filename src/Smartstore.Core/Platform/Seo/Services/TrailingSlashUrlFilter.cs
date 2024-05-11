using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Seo
{
    public class TrailingSlashUrlFilter : IUrlFilter
    {
        private readonly RouteOptions _routeOptions;

        public TrailingSlashUrlFilter(IOptions<RouteOptions> routeOptions)
        {
            _routeOptions = routeOptions.Value;
        }

        public void Apply(UrlPolicy policy, HttpContext httpContext)
        {
            var rule = policy.SeoSettings.TrailingSlashRule;
            if (rule == TrailingSlashRule.Allow)
            {
                // Don't go further, we gonna create a canonical link anyway.
                return;
            }

            if (policy.Endpoint == null)
            {
                // Apply rule only if an endpoint was matched.
                return;
            }

            var pathValue = policy.Path.Value;
            if (string.IsNullOrEmpty(pathValue) || pathValue == "/")
            {
                // Don't apply rule to homepage.
                return;
            }

            if (!httpContext.Request.IsNonAjaxGet())
            {
                // Apply rule to non-ajax GET requests only.
                return;
            }

            if (httpContext.GetRouteValueAs<string>("area").HasValue())
            {
                // Apply rule to public store only.
                return;
            }

            // Don't read this setting from SeoSettings because it can change
            // during the app lifecycle without taking effect.
            var shouldAppendTrailingSlash = _routeOptions.AppendTrailingSlash;
            var hasTralingSlash = policy.OriginalPath.Value.EndsWith('/');

            if (hasTralingSlash != shouldAppendTrailingSlash)
            {
                if (rule == TrailingSlashRule.Disallow)
                {
                    policy.IsInvalidUrl = true;
                }
                else if (rule == TrailingSlashRule.RedirectToHome)
                {
                    policy.Path.Modify("/");
                }
                else if (rule == TrailingSlashRule.Redirect)
                {
                    var newPath = shouldAppendTrailingSlash
                        ? pathValue.EnsureEndsWith('/')
                        : pathValue.TrimEnd('/');

                    policy.Path.Modify(newPath);
                }
            }
        }
    }
}
