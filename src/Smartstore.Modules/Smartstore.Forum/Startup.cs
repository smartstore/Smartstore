using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Caching;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Forum.Data;

namespace Smartstore.Forum
{
    //internal class Startup : StarterBase
    //{
    //    public override bool Matches(IApplicationContext appContext)
    //        => appContext.IsInstalled;

    //    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
    //    {
    //        if (!isActiveModule)
    //        {
    //            return;
    //        }

    //        "-- AddDbContext<ForumDbContext>".Dump();

    //        services.AddDbContext<ForumDbContext>(builder => builder
    //            .UseSecondLevelCache()
    //            .UseDbFactory());
    //    }

    //    public override void MapRoutes(EndpointRoutingBuilder builder)
    //    {
    //        //builder.MapRoutes(0, routes => 
    //        //{
    //        //    //routes.MapControllerRoute("SmartStore.Forum",
    //        //    //     "Module/Smartstore.Forum/{action=Configure}/{id?}"
    //        //    //);
    //        //});
    //    }

    //}
}
