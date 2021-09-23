using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Forums.Filters;
using Smartstore.Forums.Search;
using Smartstore.Forums.Search.Modelling;
using Smartstore.Forums.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.Forums
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            services.AddScoped<IForumService, ForumService>();
            services.AddScoped<IXmlSitemapPublisher, ForumService>();

            // Search.
            services.AddScoped<IForumSearchQueryAliasMapper, ForumSearchQueryAliasMapper>();
            services.AddScoped<IForumSearchQueryFactory, ForumSearchQueryFactory>();
            services.AddScoped<IForumSearchService, ForumSearchService>();

            // TODO: (mg) (core) register LinqForumSearchService. Was registered by name before.
            //builder.RegisterType<LinqForumSearchService>().Named<IForumSearchService>("linq").InstancePerRequest();

            SlugRouteTransformer.RegisterRouter(new ForumSlugRouter());

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<ForumMenuItemFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest(), 300);
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
