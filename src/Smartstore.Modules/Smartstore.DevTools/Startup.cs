using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.DevTools.Filters;
using Smartstore.DevTools.Services;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;

namespace Smartstore.DevTools
{
    internal class Startup : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            services.AddScoped<IChronometer, MiniProfilerChronometer>();
            //services.AddScoped<IFacetTemplateSelector, CustomFacetTemplateSelector>();

            services.AddMiniProfiler(o =>
            {
                //o.EnableDebugMode = true;
                o.EnableMvcFilterProfiling = true;
                o.EnableMvcViewProfiling = true;
                o.MaxUnviewedProfiles = 5;

                o.ShouldProfile = ShouldProfile;
                o.ResultsAuthorize = ResultsAuthorize;
                o.ResultsListAuthorize = ResultsAuthorize;

                o.IgnoredPaths.Clear();
                o.IgnorePath("/favicon.ico");

                //// INFO: Handled by settings now.
                //o.IgnorePath("/admin/");
                //o.IgnorePath("/themes/");
                //o.IgnorePath("/taskscheduler/");
                //o.IgnorePath("/bundle/");
                //o.IgnorePath("/media/");
                //o.IgnorePath("/js/");
                //o.IgnorePath("/css/");
                //o.IgnorePath("/images/");
            }).AddEntityFramework();

            services.Configure<MvcOptions>(o =>
            {
                var originalFilter = o.Filters.FirstOrDefault(x => x is ProfilingActionFilter);
                if (originalFilter != null)
                {
                    // Remove the original filter, we built a custom one.
                    o.Filters.Remove(originalFilter);
                }

                o.Filters.AddConditional<MiniProfilerFilter>(
                    context => context.ControllerIs<SmartController>() && ShouldProfile(context.HttpContext.Request));

                o.Filters.AddConditional<MachineNameFilter>(
                    context => context.ControllerIs<SmartController>() && context.HttpContext.Request.IsNonAjaxGet());

                o.Filters.AddConditional<WidgetZoneFilter>(
                    context => context.ControllerIs<SmartController>() && context.HttpContext.Request.IsNonAjaxGet());

                //o.Filters.AddConditional<SampleProductDetailActionFilter>(
                //    context => context.ControllerIs<ProductController>());

                //o.Filters.AddConditional<SampleResultFilter>(
                //    context => context.ControllerIs<CatalogController>());

                //o.Filters.AddConditional<SampleActionFilter>(
                //    context => context.ControllerIs<PublicController>());

                //o.Filters.AddConditional<SampleCheckoutFilter>(
                //    context => context.ControllerIs<CheckoutController>());

            });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.AfterWorkContextMiddleware, app =>
            {
                app.UseMiniProfiler();
            });

            // OutputCache invalidation configuration
            var observer = builder.ApplicationBuilder.ApplicationServices.GetRequiredService<IOutputCacheInvalidationObserver>();
            observer.ObserveSettingProperty<ProfilerSettings>(x => x.DisplayMachineName);
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            //builder.MapRoutes(0, routes =>
            //{
            //    //routes.MapControllerRoute("Smartstore.DevTools",
            //    //     "devtools/{action=Configure}/{id?}"
            //    //);
            //});
        }

        internal static bool ShouldProfile(HttpRequest request)
        {
            var services = request.HttpContext.RequestServices;
            var settings = services.GetRequiredService<ProfilerSettings>();

            if (!settings.EnableMiniProfilerInPublicStore)
            {
                return false;
            }

            var ua = services.GetRequiredService<IUserAgent>();
            if (ua.IsMobileDevice && !ua.IsTablet)
            {
                return false;
            }

            if (!request.HttpContext.Connection.IsLocal() && !services.GetRequiredService<IWorkContext>().CurrentCustomer.IsAdmin())
            {
                return false;
            }

            var ignorePaths = settings.MiniProfilerIgnorePaths;
            if (ignorePaths != null && ignorePaths.Length > 0)
            {
                var currentPath = request.Path.Value;
                foreach (var ignorePath in ignorePaths)
                {
                    if (currentPath.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool ResultsAuthorize(HttpRequest request)
        {
            var ua = request.HttpContext.RequestServices.GetRequiredService<IUserAgent>();
            if (ua.IsPdfConverter || ua.IsBot)
            {
                return false;
            }

            return request.HttpContext.RequestServices.GetRequiredService<IWorkContext>().CurrentCustomer.IsAdmin();
        }

        class SmartDbContextConfigurer : IDbContextConfigurationSource<SmartDbContext>
        {
            public void Configure(IServiceProvider services, DbContextOptionsBuilder builder)
            {
                builder.UseDbFactory(b =>
                {
                    b.AddModelAssembly(this.GetType().Assembly);
                });
            }
        }
    }
}