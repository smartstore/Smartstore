using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Engine;

namespace Smartstore.Web.Common
{
    public class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddTransient<IWorkContext, WebWorkContext>();
            services.AddScoped<SlugRouteTransformer>();
        }

        public override int PipelineOrder => (int)StarterOrdering.Early;
        public override void BuildPipeline(IApplicationBuilder app, IApplicationContext appContext)
        {
            app.Map("/sitemap.xml", true, b => b.UseMiddleware<XmlSitemapMiddleware>());
        }

        public override int RoutesOrder => (int)StarterOrdering.Early;
        public override void MapRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            if (!appContext.IsInstalled)
            {
                return;
            }

            routes.MapDynamicControllerRoute<SlugRouteTransformer>("{**SeName:minlength(2)}");
        }
    }

    public class LastRoutes : StarterBase
    {
        public override bool Matches(IApplicationContext appContext) => appContext.IsInstalled;
        public override int RoutesOrder => (int)StarterOrdering.Last;
        public override void MapRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            // Register routes from SlugRouteTransformer solely needed for URL creation, NOT for route matching.
            SlugRouteTransformer.Routers.Each(x => x.MapRoutes(routes));

            // TODO: (core) Very last route: PageNotFound?
            routes.MapControllerRoute("PageNotFound", "{*path}", new { controller = "Error", action = "NotFound" });
        }
    }
}