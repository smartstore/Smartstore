using System;
using System.Threading.Tasks;
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

        public UrlPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, IUrlService urlService, IWorkContext workContext)
        {
            var policy = urlService.GetUrlPolicy();

            if (policy.IsInvalidUrl)
            {
                // TODO: (core) Handle 404 result decently. See LanguageSeoCodeAttribute.OnAuthorization > HandleExceptionFilter.Create404Result()
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            if (policy.IsModified)
            {
                return HandleRedirect(policy.GetModifiedUrl());
            }

            policy = urlService.ApplyCanonicalUrlRulesPolicy();

            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                policy = urlService.ApplyCultureUrlPolicy(endpoint);
            }

            if (policy.IsModified)
            {
                return HandleRedirect(policy.GetModifiedUrl());
            }

            policy.WorkingLanguage = workContext.WorkingLanguage;

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