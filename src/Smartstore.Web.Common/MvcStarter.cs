using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.DependencyInjection;
using Smartstore.Core.Localization.Routing;
using Smartstore.Engine;

namespace Smartstore.Web.Common
{
    public class MvcStarter : StarterBase
    {
        public MvcStarter()
        {
            RunAfter<WebStarter>();
        }
        
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

            services.AddRouting(o => 
            {
                // TODO: (core) Make this behave like in SMNET
                o.AppendTrailingSlash = true;
                o.LowercaseUrls = true;
            });

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
                // TODO: (core) Add FluentValidation
                .AddNewtonsoftJson(o =>
                {
                    // TODO: (core) Do some ContractResolver stuff
                })
                .AddControllersAsServices()
                .AddAppLocalization()
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

        public override int PipelineOrder => (int)StarterOrdering.Early;
        public override void BuildPipeline(IApplicationBuilder app, IApplicationContext appContext)
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

            //app.Use(async (context, next) =>
            //{
            //    var path = context.Request.Path.Value;
            //    var helper = new LocalizedUrlHelper(context.Request);
            //    if (helper.IsLocalizedUrl(out var seoCode))
            //    {
            //        helper.StripSeoCode();
            //        context.GetRouteData().DataTokens["culture"] = seoCode;
            //        context.Request.Path = helper.GetAbsolutePath();
            //    }

            //    await next();
            //});

            app.UseRouting();

            // TODO: (core) Use Swagger
            // TODO: (core) Use Response compression

            // TODO: (core) Use media middleware
            //app.UseSession(); // TODO: (core) Configure session

            if (appContext.IsInstalled)
            {
                app.UseAppRequestLocalization();
                app.UseCultureMiddleware();
            }

            app.UseCookiePolicy(); // TODO: (core) Configure cookie policy

            app.UseAuthorization(); // TODO: (core) Configure custom auth with Identity Server
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterDecorator<SmartLinkGenerator, LinkGenerator>();
        }

        public override int RoutesOrder => (int)StarterOrdering.Early;
        public override void MapRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            //routes.MapControllerRoute(
            //    name: "areas",
            //    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            routes.MapControllers();

            //routes.MapControllerRoute(
            //    name: "default-localized",
            //    pattern: "{culture:culture=de}/{controller=Home}/{action=Index}/{id?}");
            routes.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}