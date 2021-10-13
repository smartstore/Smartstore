using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Forums.Filters;
using Smartstore.Forums.Search;
using Smartstore.Forums.Search.Facets;
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
            services.AddScoped<LinqForumSearchService>();
            services.AddScoped<IFacetUrlHelper, ForumFacetUrlHelper>();

            SlugRouteTransformer.RegisterRouter(new ForumSlugRouter());

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<ForumMenuItemFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest(), 300);

                o.Filters.AddConditional<PmAccountDropdownFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest());

                o.Filters.AddConditional<PmMessagingDropdownFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("Admin", "Customer", "Edit") ?? false);

                o.Filters.AddConditional<CustomerInfoFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("Customer", "Info") ?? false);

                o.Filters.AddConditional<CustomerProfileFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("Profile", "Index") ?? false);
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
