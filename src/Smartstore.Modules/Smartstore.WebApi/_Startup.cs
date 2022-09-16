using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
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

            mvcBuilder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Smartstore Web API"
                    });

                    options.CustomSchemaIds(type => type.FullName);
                    // TODO: (mg) (core) add ODataOperationFilter.
                    //options.OperationFilter<ODataOperationFilter>();

                    try
                    {
                        // INFO: enable "Documentation file" in project properties. Leave file path empty.
                        // Optionally append 1591 to "Suppress specific warnings" to suppress warning about missing XML comments.
                        var fileName = Assembly.GetExecutingAssembly().GetName().Name;
                        var xmlFile = appContext.ModulesRoot.GetFile($"{Module.SystemName}/{fileName}.xml");

                        // TODO: (mg) (core) we probably need an XML doc factory because code comments are spread over several projects.
                        options.IncludeXmlComments(xmlFile.PhysicalPath, true);
                    }
                    catch (Exception ex)
                    {
                        ex.Dump();
                    }
                });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            if (builder.ApplicationContext.IsInstalled)
            {
                // INFO: (mg) (core) You should always THOROUGHLY (!!!) think about the correct ordering of every single middleware!
                // It can have HEAVY impact on performance, security and functionality.
                // See https://github.com/OData/AspNetCoreOData/blob/master/docs/odatamiddelware.md

                builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
                {
                    // INFO: (mg) (core) We had this issue already. You just can't register a middleware twice. It will be executed twice!
                    //if (builder.ApplicationContext.HostEnvironment.IsDevelopment())
                    //{
                    //    app.UseDeveloperExceptionPage();
                    //}

                    // TODO: (mg) (core) Not sure whether this is the correct ordering for Swagger?! Investigate please.
                    app.UseSwagger(options =>
                    {
                    });

                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");

                        // Hide schemas dropdown.
                        options.DefaultModelsExpandDepth(-1);
                        options.EnableTryItOutByDefault();
                        //options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
                    });
                });

                builder.Configure(StarterOrdering.BeforeRoutingMiddleware, app => 
                {
                    // Use odata route debug, /$odata
                    // TODO: (mg) (core) Use only in dev mode?
                    app.UseODataRouteDebug();

                    // Add OData /$query middleware.
                    app.UseODataQueryRequest();

                    // Add the OData Batch middleware to support OData $Batch.
                    //app.UseODataBatching();

                    // If you want to use /$openapi, enable the middleware.
                    //app.UseODataOpenApi();
                });
            }
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddAuthentication("Smartstore.WebApi.Basic")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Smartstore.WebApi.Basic", null);
        }
    }
}
