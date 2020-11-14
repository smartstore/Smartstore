using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Engine;

namespace Smartstore.Web.Common
{
    public class MvcStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
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
        }

        public override int ApplicationOrder => int.MinValue + 100;
        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            if (appContext.HostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles(); // TODO: (core) Set StaticFileOptions
            app.UseRouting();
            // TODO: (core) Use Swagger
            app.UseCookiePolicy(); // TODO: (core) Configure cookie policy
            app.UseAuthorization(); // TODO: (core) Configure custom auth with Identity Server
            // TODO: (core) Use request localization
            // TODO: (core) Use SEO url rewriter
            // TODO: (core) Use media middleware
            app.UseRequestLocalization(); // TODO: (core) Configure request localization
            //app.UseSession(); // TODO: (core) Configure session
        }

        public override int RoutesOrder => -100;
        public override void ConfigureRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            routes.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            routes.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}