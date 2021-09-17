using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Blog.Filters;
using Smartstore.Blog.Services;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;

namespace Smartstore.Blog
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<ILinkProvider, BlogLinkProvider>();

            SlugRouteTransformer.RegisterRouter(new BlogSlugRouter());

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<BlogMenuItemFilter>(
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
