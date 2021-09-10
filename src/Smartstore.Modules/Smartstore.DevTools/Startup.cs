using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.DevTools.Filters;
using Smartstore.DevTools.Services;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
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

            services.AddMiniProfiler(o =>
            {
                //o.EnableDebugMode = true;
                o.EnableMvcFilterProfiling = true;
                o.EnableMvcViewProfiling = true;
                o.EnableServerTimingHeader = true;
                o.MaxUnviewedProfiles = 5;

                o.ShouldProfile = ShouldProfile;
                o.ResultsAuthorize = ResultsAuthorize;
                o.ResultsListAuthorize = ResultsAuthorize;

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
                
                o.Filters.Add<ProfilerFilter>();
                o.Filters.Add<MachineNameFilter>();
            });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.FirstMiddleware, app =>
            {
                app.UseMiniProfiler();
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            //builder.MapRoutes(0, routes => 
            //{
            //    //routes.MapControllerRoute("SmartStore.DevTools",
            //    //     "Module/Smartstore.DevTools/{action=Configure}/{id?}"
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
