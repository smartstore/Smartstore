using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.News.Controllers;
using Smartstore.News.Filters;
using Smartstore.News.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.News
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            services.AddScoped<IXmlSitemapPublisher, NewsItemXmlSitemapPublisher>();
            services.AddScoped<ILinkProvider, NewsLinkProvider>();

            SlugRouteTransformer.RegisterRouter(new NewsSlugRouter());

            // Output cache display control
            DisplayControl.RegisterHandlerFor(typeof(NewsItem), (x, d, c)
                => Task.FromResult<IEnumerable<string>>(new[] { "n" + x.Id }));

            DisplayControl.RegisterHandlerFor(typeof(NewsComment), (x, d, c)
                => Task.FromResult<IEnumerable<string>>(new[] { "n" + ((NewsComment)x).NewsItemId }));

            if (appContext.IsInstalled)
            {
                services.AddScoped<NewsHelper>();
            }

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<NewsMenuItemFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest(), 100);

                o.Filters.AddConditional<RssHeaderLinkFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest());
            });
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
