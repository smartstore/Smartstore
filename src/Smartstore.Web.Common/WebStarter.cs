using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Core;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net;
using Smartstore.Web.Bootstrapping;
using Smartstore.Web.Razor;

namespace Smartstore.Web
{
    internal class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                // Configure Cookie Policy Options
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CookiePolicyOptions>, CookiePolicyOptionsConfigurer>());
            }

            // Add AntiForgery
            services.AddAntiforgery(o =>
            {
                o.Cookie.Name = CookieNames.Antiforgery;
                o.HeaderName = "X-XSRF-Token";
            });

            // Add HTTP client feature
            services.AddHttpClient(string.Empty, client => 
            {
                client.DefaultRequestHeaders.Add("User-Agent", $"Smartstore {SmartstoreVersion.CurrentFullVersion}");
            });

            // Add session feature
            services.AddSession(o =>
            {
                o.Cookie.Name = CookieNames.Session;
                o.Cookie.IsEssential = true;
            });

            // Detailed database related error notifications
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.Configure<WebEncoderOptions>(o =>
            {
                o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<DefaultViewInvoker>().As<IViewInvoker>().InstancePerLifetimeScope();
            builder.RegisterType<WebWorkContext>().As<IWorkContext>().InstancePerLifetimeScope();
            builder.RegisterType<SlugRouteTransformer>().InstancePerLifetimeScope();
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            if (!builder.ApplicationContext.IsInstalled)
            {
                return;
            }

            builder.MapRoutes(StarterOrdering.EarlyRoute, routes => 
            {
                routes.MapDynamicControllerRoute<SlugRouteTransformer>("{**slug:minlength(2)}");
            });
        }
    }

    internal class LastRoutes : StarterBase
    {
        public override bool Matches(IApplicationContext appContext) 
            => appContext.IsInstalled;

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.LastRoute, routes =>
            {
                // Register routes from SlugRouteTransformer solely needed for URL creation, NOT for route matching.
                SlugRouteTransformer.Routers.Each(x => x.MapRoutes(routes));

                // TODO: (core) Very last route: PageNotFound?
                routes.MapControllerRoute("PageNotFound", "{*path}", new { controller = "Error", action = "NotFound" });
            });
        }
    }
}