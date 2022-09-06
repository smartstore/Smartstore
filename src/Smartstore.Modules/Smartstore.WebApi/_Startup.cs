using Autofac;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.NewtonsoftJson;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.WebApi
{
    internal class Startup : StarterBase
    {
        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            //services.TryAddEnumerable(ServiceDescriptor.Transient<IODataControllerActionConvention, CustomRoutingConvention>());

            mvcBuilder
                .AddODataNewtonsoftJson()
                .AddOData(options =>
                {
                    options
                        .EnableQueryFeatures(WebApiSettings.DefaultMaxTop)
                        .AddRouteComponents("odata/v1", EdmBuilder.BuildV1Model());

                    //options.EnableAttributeRouting = true;
                    //options.Conventions.Insert(0, new CustomRoutingConvention());
                });
        }

        //public override void BuildPipeline(RequestPipelineBuilder builder)
        //{
        //    if (builder.ApplicationContext.HostEnvironment.IsDevelopment())
        //    {
        //        builder.ApplicationBuilder.UseODataRouteDebug();
        //    }
        //}

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<WebApiService>().As<IWebApiService>().SingleInstance();
        }
    }
}
