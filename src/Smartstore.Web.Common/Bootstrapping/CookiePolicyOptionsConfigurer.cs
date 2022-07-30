using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;
using Smartstore.Net;

namespace Smartstore.Web.Bootstrapping
{
    internal class CookiePolicyOptionsConfigurer : IConfigureOptions<CookiePolicyOptions>
    {
        public void Configure(CookiePolicyOptions options)
        {
            //// TODO: (core) Configure CookiePolicyOptions including GDPR consent stuff.
            //// TODO: (core) Update CookiePolicyOptions whenever store or other dependent settings change by calling this method from controller with current options.

            //options.CheckConsentNeeded = context => false;
            options.HttpOnly = HttpOnlyPolicy.None;
            options.ConsentCookie.Name = CookieNames.CookieConsent;
            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;

            // Whether to allow the use of cookies from SSL protected page on the other store pages which are not protected
            options.Secure = CookieSecurePolicy.SameAsRequest;

            options.OnAppendCookie = cookieContext => ApplyCookiePolicy(cookieContext.Context, cookieContext.CookieOptions);
            options.OnDeleteCookie = cookieContext => ApplyCookiePolicy(cookieContext.Context, cookieContext.CookieOptions);
        }

        private static void ApplyCookiePolicy(HttpContext httpContext, CookieOptions options)
        {
            if (httpContext.Request.PathBase.HasValue)
            {
                options.Path = httpContext.Request.PathBase;
            }

            var webHelper = httpContext.RequestServices.GetRequiredService<IWebHelper>();
            var privacySettings = httpContext.RequestServices.GetRequiredService<PrivacySettings>();
            var isSecured = webHelper.IsCurrentConnectionSecured();

            options.Secure = isSecured;
            options.SameSite = isSecured ? privacySettings.SameSiteMode : SameSiteMode.Lax;

            // Check SameSite backwards compatiblity
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (userAgent.IsEmpty())
                {
                    return;
                }

                // Cover all iOS based browsers here. This includes:
                // - Safari on iOS 12 for iPhone, iPod Touch, iPad
                // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
                // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
                // All of which are broken by SameSite=None, because they use the iOS networking stack
                if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                    return;
                }

                // Cover Mac OS X based browsers that use the Mac OS networking stack. This includes:
                // - Safari on Mac OS X.
                // This does not include:
                // - Chrome on Mac OS X
                // Because they do not use the Mac OS networking stack.
                if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                    userAgent.Contains("Version/") && userAgent.Contains("Safari"))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                    return;
                }

                // Cover Chrome 50-69, because some versions are broken by SameSite=None,
                // and none in this range require it.
                // Note: this covers some pre-Chromium Edge versions,
                // but pre-Chromium Edge does not require SameSite=None.
                if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }
    }
}
