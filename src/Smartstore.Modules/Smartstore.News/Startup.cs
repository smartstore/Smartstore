using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.News.Filters;
using Smartstore.News.Services;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;
using Smartstore.Core.Seo;
using Smartstore.News.Controllers;
using Autofac;

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
