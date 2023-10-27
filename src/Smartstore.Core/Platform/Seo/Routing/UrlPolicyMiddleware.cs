using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Smartstore.Utilities;

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
                    return HandleRedirect(policy);
                }
                
                // We may need the original endpoint for logging and error handling purposes later,
                // but the ExeptionHandler middleware sets endpoint to null in order to re-execute correctly.
                // Therefore we gonna save it here, but only if we're not in re-execution.
                policy.Endpoint ??= context.GetEndpoint();

                // Apply all registered url filters
                foreach (var urlFilter in _urlFilters)
                {
                    if (!policy.IsInvalidUrl)
                    {
                        urlFilter.Apply(policy, context);
                    }
                }

                // Check again after url filters have been applied
                if (policy.IsInvalidUrl)
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }
                else if (policy.IsModified)
                {
                    return HandleRedirect(policy);
                }
            }

            // No redirection was requested. Continue.
            return _next(context);

            Task HandleRedirect(UrlPolicy policy)
            {
                var isPermanent = 
                    // Never make permanent redirect in a dev environment or on localhost...
                    !CommonHelper.IsDevEnvironment &&
                    // ...also: a redirect from a culture-less URL to a culture-specific URL must never be permanent,
                    // because the browser will cache this info, what blocks the way when we want to switch back again.
                    (!policy.Culture.IsModified || policy.Culture.Original.HasValue());

                context.Response.StatusCode = policy.Scheme.IsModified
                    ? (isPermanent ? StatusCodes.Status301MovedPermanently : StatusCodes.Status302Found)
                    // Use 307/308 for HTTPS redirection
                    : (isPermanent ? StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect);

                context.Response.Headers[HeaderNames.Location] = policy.GetModifiedUrl();

                return Task.CompletedTask;
            }
        }
    }
}