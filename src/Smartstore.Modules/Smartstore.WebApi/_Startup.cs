using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Api.Security;

namespace Smartstore.Web.Api
{
    // TODO: (mg) (core) Enable OData batching via app.UseODataBatching(). See: https://devblogs.microsoft.com/odata/tutorial-creating-a-service-with-odata-8-0/
    // PS: that document looks outdated. MapODataRoute and ODataOptions.AddModel does not exist anymore.

    internal class Startup : StarterBase
    {
        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            //services.TryAddEnumerable(ServiceDescriptor.Transient<IODataControllerActionConvention, CustomRoutingConvention>());

            mvcBuilder
                .AddODataNewtonsoftJson()
                .AddOData(options =>
                {
                    var modelBuilder = new ODataConventionModelBuilder();
                    var modelProviders = appContext.TypeScanner
                        .FindTypes<IODataModelProvider>()
                        .Select(x => (IODataModelProvider)Activator.CreateInstance(x));

                    foreach (var provider in modelProviders)
                    {
                        provider.Build(modelBuilder, 1);
                    }

                    var edmModel = modelBuilder.GetEdmModel();
                    
                    options
                        .EnableQueryFeatures(WebApiSettings.DefaultMaxTop)
                        .AddRouteComponents("odata/v1", edmModel);

                    options.TimeZone = TimeZoneInfo.Utc;
                    options.RouteOptions.EnableUnqualifiedOperationCall = true;

                    //options.EnableAttributeRouting = true;
                    //options.Conventions.Add(new CustomRoutingConvention());
                });
        }

        //public override void BuildPipeline(RequestPipelineBuilder builder)
        //{
        //    if (builder.ApplicationContext.HostEnvironment.IsDevelopment())
        //    {
        //        builder.ApplicationBuilder.UseODataRouteDebug();
        //    }
        //}

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddAuthentication("Smartstore.WebApi.Basic")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Smartstore.WebApi.Basic", null);
        }
    }
}
