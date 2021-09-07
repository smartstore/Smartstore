using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Blog.Data;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Blog
{
    internal class Startup : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            //services.AddScoped<IBlogService, BlogService>();

            //if (!appContext.IsInstalled)
            //{
            //    services.AddSingleton<IDbContextFactory<BlogDbContext>>(
            //        new SimpleDbContextFactory<BlogDbContext>(appContext.AppConfiguration.DbMigrationCommandTimeout));
            //}
            //else
            //{
            //    // Application DbContext as pooled factory
            //    services.AddPooledDbContextFactory<BlogDbContext>((c, builder) =>
            //    {
            //        builder
            //            .UseDbFactory();
            //    }, appContext.AppConfiguration.DbContextPoolSize);
            //}

            //services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<BlogDbContext>>().CreateDbContext());
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
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
    }
}
