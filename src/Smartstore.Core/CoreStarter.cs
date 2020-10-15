using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Web;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Caching;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Seo.Services;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Engine;
using Smartstore.Web;

namespace Smartstore.Core
{
    public class CoreStarter : StarterBase
    {
        // TODO: (core) Modularize the starters
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            var appConfig = appContext.AppConfiguration;

            services.AddSingleton<MemoryCacheStore>();

            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddAntiforgery(o => o.HeaderName = "X-XSRF-Token");

            // TODO: (core) Configuration for MemoryCache?
            services.AddMemoryCache();

            services.AddScoped<IStoreContext, StoreContext>();
            services.AddScoped<IWebHelper, WebHelper>();
            services.AddScoped<IMeasureService, MeasureService>();
            services.AddScoped<SeoSlugRouteValueTransformer>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.AddDbContextFactory<SmartDbContext>();
            services.AddPooledDbContextFactory<SmartDbContext>(ConfigureDbContext, appConfig.DbContextPoolSize);
            //services.AddDbContextPool<SmartDbContext>(ConfigureDbContext, appConfig.DbContextPoolSize);
            services.AddScoped<SmartDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());

            services.Configure<RazorViewEngineOptions>(o =>
            {
                // TODO: (core) Register view location formats/expanders
            });

            services.AddMiniProfiler(o =>
            {
                // TODO: (more) Move to module and configure
            }).AddEntityFramework();

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

            if (appConfig.UseSessionStateTempDataProvider)
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

        private static void ConfigureDbContext(IServiceProvider p, DbContextOptionsBuilder o)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;

            //// TODO: (core) Fetch ConnectionString from tenant settings
            //// TODO: (core) Fetch services which SmartDbContext depends on from IInfrastructure<IServiceProvider>
            //o.UseSqlServer(appContext.Configuration.GetConnectionString("DefaultConnection"), sql =>
            //{
            //    if (appConfig.DbCommandTimeout.HasValue)
            //    {
            //        sql.CommandTimeout(appConfig.DbCommandTimeout.Value);
            //    }
            //});
            o.UseSqlServer(DataSettings.Instance.ConnectionString, sql =>
            {
                if (appConfig.DbCommandTimeout.HasValue)
                {
                    sql.CommandTimeout(appConfig.DbCommandTimeout.Value);
                }
            })
            .ConfigureWarnings(w =>
            {
                // EF throws when query is untracked otherwise
                w.Ignore(CoreEventId.DetachedLazyLoadingWarning);
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterGeneric(typeof(WorkValues<>)).InstancePerLifetimeScope();
            builder.RegisterSource(new WorkSource());

            builder.RegisterType<SettingService>()
                .As<ISettingService>()
                .InstancePerLifetimeScope();
        }

        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            app.UseMiniProfiler();

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
            app.UsePolyfillHttpContext();
            app.UseRequestLocalization(); // TODO: (core) Configure request localization
            //app.UseSession(); // TODO: (core) Configure session
        }

        public override void ConfigureRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            routes.MapDynamicControllerRoute<SeoSlugRouteValueTransformer>("{**slug:minlength(2)}");
            routes.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
            routes.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
