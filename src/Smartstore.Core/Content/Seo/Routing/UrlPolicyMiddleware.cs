using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Core.Content.Seo.Routing
{
    /// <summary>
    /// Determines current URL policy and performs HTTP redirection
    /// if any previous middleware required redirection to a new
    /// valid / sanitized location.
    /// </summary>
    public class UrlPolicyMiddleware
    {
        private readonly RequestDelegate _next;

        public UrlPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, IUrlService urlService, IWorkContext workContext)
        {
            if (context.Response.StatusCode is (>= 200 and < 300))
            {
                var policy = urlService.GetUrlPolicy();

                if (policy.IsInvalidUrl)
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }
                else if (policy.IsModified)
                {
                    return HandleRedirect(policy.GetModifiedUrl());
                }

                policy = urlService.ApplyCanonicalUrlRulesPolicy();

                var endpoint = context.GetEndpoint();
                if (endpoint != null)
                {
                    policy = urlService.ApplyCultureUrlPolicy(endpoint);
                }

                // Check again after policies has been applied
                if (policy.IsInvalidUrl)
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }
                else if (policy.IsModified)
                {
                    return HandleRedirect(policy.GetModifiedUrl());
                }

                policy.WorkingLanguage = workContext.WorkingLanguage;

                if (policy.Endpoint == null)
                {
                    // We may need the original endpoint for logging and error handling purposes later,
                    // but the ExeptionHandler middleware sets endpoint to null in order to re-execute correctly.
                    // Therefore we gonna save it here, but only if we're not in re-execution.
                    policy.Endpoint = context.GetEndpoint();
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