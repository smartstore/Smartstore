using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

using Smartstore.IconKit;

namespace Smartstore.Bootstrapping;

internal class IconKitStarter : StarterBase
{
    /// <inheritdoc />
    public override int Order => StarterOrdering.Early;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddIconKit();
    }

    /// <inheritdoc />
    public override void MapRoutes(EndpointRoutingBuilder builder)
    {
        builder.MapRoutes(StarterOrdering.EarlyRoute, routes => routes.MapIconKit());
    }
}
