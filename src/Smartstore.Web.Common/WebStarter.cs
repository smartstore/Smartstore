using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Seo;
using Smartstore.Engine;

namespace Smartstore.Web.Common
{
    public class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddTransient<IWorkContext, WebWorkContext>();
            services.AddScoped<SeoSlugRouteValueTransformer>();
        }

        public override int ApplicationOrder => int.MinValue + 200;
        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            app.Map("/sitemap.xml", true, b => b.UseMiddleware<XmlSitemapMiddleware>());
        }

        public override int RoutesOrder => -1000;
        public override void ConfigureRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
            if (!appContext.IsInstalled)
            {
                return;
            }

            routes.MapDynamicControllerRoute<SeoSlugRouteValueTransformer>("{**slug:minlength(2)}");

            routes.MapControllerRoute("Homepage", "",
                new { controller = "Home", action = "Index" });
        }
    }
}