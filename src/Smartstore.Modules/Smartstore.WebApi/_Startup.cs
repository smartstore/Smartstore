using Microsoft.AspNetCore.Builder;
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
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.Configure<IMvcBuilder>(builder =>
            {
                builder
                    .AddODataNewtonsoftJson()
                    .AddOData(options =>
                    {
                        options
                            .EnableQueryFeatures(120)
                            .AddRouteComponents("odata/v1", EdmBuilder.BuildV1Model());
                    });
            });

            services.Configure<IApplicationBuilder>(builder =>
            {
                if (appContext.HostEnvironment.IsDevelopment())
                {
                    builder.UseODataRouteDebug();
                }
            });
        }
    }
}
