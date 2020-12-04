using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Ensures that store URL contains a language SEO code if "SEO friendly URLs with multiple languages" setting is enabled
    /// </summary>
    public class CultureRedirectionMiddleware
    {
        private readonly RequestDelegate _next;

        public CultureRedirectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, IWorkContext workContext, ILanguageService languageService, LocalizationSettings localizationSettings)
        {
            if (context.Items.TryGetValue("__RedirectLocation", out var value) && value is string redirectLocation)
            {
                // Any middleware or transformer before this one requests a redirection.
                return HandleRedirect(redirectLocation);
            }

            var request = context.Request;

            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled || request.Method != HttpMethod.Get.Method)
            {
                // Handle only GET requests and when config says so.
                return _next(context);
            }

            var cultureCode = context.GetCultureCode(out var localizedRouteMetadata);
            if (localizedRouteMetadata == null)
            {
                // Handle only localizable routes
                return _next(context);
            }

            var workingLanguage = workContext.WorkingLanguage;
            var helper = new LocalizedUrlHelper(request);
            var defaultCultureCode = languageService.GetDefaultLanguageSeoCode();
            var invalidBehavior = localizationSettings.InvalidLanguageRedirectBehaviour;
            var defaultBehavior = localizationSettings.DefaultLanguageRedirectBehaviour;

            if (cultureCode.HasValue())
            {
                if (!languageService.IsPublishedLanguage(cultureCode))
                {
                    // Language is not defined in system or not assigned to store
                    if (invalidBehavior == InvalidLanguageRedirectBehaviour.ReturnHttp404)
                    {
                        var cultureCodeReplacement = defaultBehavior == DefaultLanguageRedirectBehaviour.PrependSeoCodeAndRedirect
                            ? workingLanguage.GetTwoLetterISOLanguageName()
                            : string.Empty;

                        context.GetRouteData().DataTokens["CultureCodeReplacement"] = cultureCodeReplacement;

                        return Handle404();
                    }
                    else if (invalidBehavior == InvalidLanguageRedirectBehaviour.FallbackToWorkingLanguage)
                    {
                        if (defaultBehavior == DefaultLanguageRedirectBehaviour.StripSeoCode)
                        {
                            helper.StripCultureCode();
                        }
                        else
                        {
                            helper.PrependCultureCode(workingLanguage.GetTwoLetterISOLanguageName(), true);
                        }

                        return HandleRedirect(helper.FullPath);
                    }
                }
                else
                {
                    // Redirect default language (if desired)
                    if (cultureCode == defaultCultureCode && defaultBehavior == DefaultLanguageRedirectBehaviour.StripSeoCode)
                    {
                        helper.StripCultureCode();
                        return HandleRedirect(helper.FullPath);
                    }
                }

                // Already localized URL, skip the rest
                return _next(context);
            }

            // Keep default language prefixless (if desired)
            if (workingLanguage.UniqueSeoCode == defaultCultureCode && (int)(defaultBehavior) > 0)
            {
                return _next(context);
            }
            
            // Add language code to URL
            helper.PrependCultureCode(workingLanguage.UniqueSeoCode);
            return HandleRedirect(helper.FullPath);

            Task HandleRedirect(string path, bool permanent = false)
            {
                context.Response.StatusCode = context.Connection.IsLocal() ? 302 : 301;
                context.Response.Headers[HeaderNames.Location] = path;
                return Task.CompletedTask;
            }

            Task Handle404()
            {
                // TODO: (core) Handle 404 result decently. See LanguageSeoCodeAttribute.OnAuthorization > HandleExceptionFilter.Create404Result()
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }
        }
    }
}
