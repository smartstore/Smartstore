using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Installation;

internal sealed class InstallationStarter : StarterBase
{
    const string InstallPath = "/install";

    public override bool Matches(IApplicationContext appContext)
        => !appContext.IsInstalled;

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        // App languages for installation are discovered from "App_Data/Seed/cultures/{culture}/culture.json".
        builder.RegisterType<InstallationService>().As<IInstallationService>().InstancePerLifetimeScope();
    }

    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        builder.Configure(StarterOrdering.EarlyMiddleware, app =>
        {
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                if (!path.StartsWithSegments(InstallPath) && !path.StartsWithSegments("/Error"))
                {
                    context.Response.Redirect(context.Request.PathBase.Value + InstallPath);
                    return;
                }

                await next();
            });
        });
    }
}