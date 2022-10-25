using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Core.Seo.Routing
{
    /// <summary>
    /// Determines current URL policy and performs HTTP redirection
    /// if any previous middleware required redirection to a new
    /// valid / sanitized location.
    /// </summary>
    public class UrlPolicyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEnumerable<IUrlFilter> _urlFilters;

        public UrlPolicyMiddleware(RequestDelegate next, IEnumerable<IUrlFilter> urlFilters)
        {
            _next = next;
            _urlFilters = urlFilters;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Response.StatusCode is (>= 200 and < 300))
            {
                var policy = context.GetUrlPolicy();

                if (policy.IsInvalidUrl)
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }
                else if (policy.IsModified)
                {
                    return HandleRedirect(policy.GetModifiedUrl());
                }

                var endpoint = context.GetEndpoint();
                if (policy.Endpoint == null)
                {
                    // We may need the original endpoint for logging and error handling purposes later,
                    // but the ExeptionHandler middleware sets endpoint to null in order to re-execute correctly.
                    // Therefore we gonna save it here, but only if we're not in re-execution.
                    policy.Endpoint = endpoint;
                }

                // Apply all registered url filters
                foreach (var urlFilter in _urlFilters)
                {
                    if (!policy.IsInvalidUrl)
                    {
                        urlFilter.Apply(policy, context);
                    }
                }

                // Check again after policies have been applied
                if (policy.IsInvalidUrl)
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }
                else if (policy.IsModified)
                {
                    return HandleRedirect(policy.GetModifiedUrl());
                }
            }

            // No redirection was requested. Continue.
            return _next(context);

            Task HandleRedirect(string path)
            {
                context.Response.StatusCode = context.Connection.IsLocal() ? 302 : 301;
                context.Response.Headers[HeaderNames.Location] = path;
                return Task.CompletedTask;
            }
        }
    }
}