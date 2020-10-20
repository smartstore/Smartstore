using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Core.Seo.Services;
using Smartstore.Engine;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebServiceCollectionExtensions
    {
        public static IServiceCollection AddSmartstoreMvc(this IServiceCollection services, IApplicationContext appContext)
        {
            services.AddScoped<SeoSlugRouteValueTransformer>();

            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddAntiforgery(o => o.HeaderName = "X-XSRF-Token");
            services.AddHttpClient();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.Configure<RazorViewEngineOptions>(o =>
            {
                // TODO: (core) Register view location formats/expanders
            });

            services.Configure<WebEncoderOptions>(o =>
            {
                o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });

            // TODO: (core) Implement localization stuff
            //services.TryAddSingleton<IStringLocalizerFactory, SmartStringLocalizerFactory>();
            //services.TryAddScoped(typeof(IStringLocalizer<>), typeof(SmartStringLocalizer<>));

            var mvcBuilder = services
                .AddControllersWithViews(o =>
                {
                    //o.EnableEndpointRouting = false;
                    // TODO: (core) AddModelBindingMessagesLocalizer
                    // TODO: (core) Add custom display metadata provider
                    // TODO: (core) Add model binders

                })
                .AddRazorRuntimeCompilation(o =>
                {
                    // TODO: (core) FileProvider
                })
                .AddDataAnnotationsLocalization(o =>
                {
                    // TODO: (core) Set DataAnnotationLocalizerProvider
                })
                // TODO: (core) Add FluentValidation
                .AddNewtonsoftJson(o =>
                {
                    // TODO: (core) Do some ContractResolver stuff
                })
                .AddControllersAsServices()
                .AddMvcOptions(o =>
                {
                    // TODO: (core) More MVC config?
                });

            if (appContext.AppConfiguration.UseSessionStateTempDataProvider)
            {
                mvcBuilder.AddSessionStateTempDataProvider();
            }
            else
            {
                mvcBuilder.AddCookieTempDataProvider(o =>
                {
                    // TODO: (core) Make all cookie names global accessible and read it from there
                    o.Cookie.Name = "Smart.TempData";

                    // Whether to allow the use of cookies from SSL protected page on the other store pages which are not protected
                    // TODO: (core) true = If current store is SSL protected
                    o.Cookie.SecurePolicy = true ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None;
                });
            }

            services.AddRazorPages();

            return services;
        }
    }
}
