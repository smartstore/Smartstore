using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.WebApi.Services;

namespace Smartstore.WebApi
{
    internal class Startup : StarterBase
    {
        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            mvcBuilder
                .AddODataNewtonsoftJson()
                .AddOData(options =>
                {
                    options
                        .EnableQueryFeatures(120)
                        .AddRouteComponents("odata/v1", EdmBuilder.BuildV1Model());
                });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            if (builder.ApplicationContext.HostEnvironment.IsDevelopment())
            {
                builder.ApplicationBuilder.UseODataRouteDebug();
            }
        }
    }
}
