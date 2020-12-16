using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

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

            services.AddDatabaseDeveloperPageExceptionFilter();

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

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterDecorator<SmartLinkGenerator, LinkGenerator>();
            builder.RegisterDecorator<SmartRouteValuesAddressScheme, IEndpointAddressScheme<RouteValuesAddress>>();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            var appContext = builder.ApplicationContext;

            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app => 
            {
                //if (appContext.HostEnvironment.IsDevelopment() || appContext.AppConfiguration.UseDeveloperExceptionPage)
                //{
                //    app.UseDeveloperExceptionPage();
                //}
                //else
                //{
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                //}

                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            });

            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                //app.UseHttpsRedirection();
                app.UseStaticFiles(); // TODO: (core) Set StaticFileOptions
            });

            builder.Configure(StarterOrdering.BeforeRoutingMiddleware, app =>
            {
                app.UseMiniProfiler();
            });

            builder.Configure(StarterOrdering.RoutingMiddleware, app =>
            {
                app.UseRouting();
            });

            builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
            {
                // TODO: (core) Use Swagger
                // TODO: (core) Use Response compression

                // TODO: (core) Use media middleware
                //app.UseSession(); // TODO: (core) Configure session
            });

            if (appContext.IsInstalled)
            {
                builder.Configure(StarterOrdering.EarlyMiddleware, app =>
                {
                    app.UseUrlPolicy();
                    app.UseRequestCulture();
                    app.UseMiddleware<SerilogHttpContextMiddleware>();
                });
            }

            builder.Configure(StarterOrdering.DefaultMiddleware, app =>
            {
                app.UseCookiePolicy(); // TODO: (core) Configure cookie policy

                app.UseAuthorization(); // TODO: (core) Configure custom auth with Identity Server
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.EarlyMiddleware, routes =>
            {
                //routes.MapControllerRoute(
                //    name: "areas",
                //    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                routes.MapXmlSitemap();

                routes.MapControllers();

                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}