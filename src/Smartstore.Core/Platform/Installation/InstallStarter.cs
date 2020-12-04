using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Installation
{
    public sealed class InstallStarter : StarterBase
    {
        const string InstallControllerName = "install";

        public override bool Matches(IApplicationContext appContext)
            => !appContext.IsInstalled;

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.EarlyMiddleware, app => 
            {
                //app.UseMiddleware<InstallMiddleware>();
                app.Use(async (context, next) =>
                {
                    var routeValues = context.GetRouteData().Values;

                    if (!routeValues.GetControllerName().EqualsNoCase(InstallControllerName))
                    {
                        context.Response.Redirect(context.Request.PathBase.Value + "/" + InstallControllerName);
                        return;
                    }

                    await next();
                });
            });
        }
    }
}
