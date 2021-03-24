using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Net;

namespace Smartstore.Web.Bootstrapping
{
    internal class CookiePolicyOptionsConfigurer : IConfigureOptions<CookiePolicyOptions>
    {
        private readonly IStoreContext _storeContext;
        private readonly PrivacySettings _privacySettings;

        public CookiePolicyOptionsConfigurer(IStoreContext storeContext, PrivacySettings privacySettings)
        {
            _storeContext = storeContext;
            _privacySettings = privacySettings;
        }

        public void Configure(CookiePolicyOptions options)
        {
            //// TODO: (core) Configure CookiePolicyOptions including GDPR consent stuff.
            //// TODO: (core) Update CookiePolicyOptions whenever store or other dependent settings change by calling this method from controller with current options.
            ///
            //options.CheckConsentNeeded = context => true;
            options.HttpOnly = HttpOnlyPolicy.None;
            options.ConsentCookie.Name = CookieNames.CookieConsent;
            options.MinimumSameSitePolicy = _privacySettings.SameSiteMode;

            // Whether to allow the use of cookies from SSL protected page on the other store pages which are not protected
            options.Secure = _storeContext.CurrentStore.SslEnabled ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None;
            //options.Secure = CookieSecurePolicy.None;

            options.OnAppendCookie = (e) =>
            {
                var webHelper = e.Context.RequestServices.GetRequiredService<IWebHelper>();
                var privacySettings = e.Context.RequestServices.GetRequiredService<PrivacySettings>();
                var isSecured = webHelper.IsCurrentConnectionSecured();

                e.CookieOptions.Secure = isSecured;
                e.CookieOptions.SameSite = isSecured ? privacySettings.SameSiteMode : SameSiteMode.Lax;

                if (e.Context.Request.PathBase.HasValue)
                {
                    e.CookieOptions.Path = e.Context.Request.PathBase;
                }
            };
        }
    }
}
