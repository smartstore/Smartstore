using System.Xml.XPath;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.Json;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Api.Security;
using Smartstore.Web.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Smartstore.Web.Api
{
    // TODO: (mg) (core) IEEE754Compatible=true is not supported\working: https://github.com/OData/WebApi/issues/1460
    // TODO: (mg) (core) check OData metadata and Swagger for unwanted entities when all is ready.

    /// <summary>
    /// For proper configuration see https://github.com/domaindrivendev/Swashbuckle.AspNetCore
    /// </summary>
    internal class Startup : StarterBase
    {
        private List<IODataModelProvider> _modelProviders;

        private List<IODataModelProvider> GetModelProviders(IApplicationContext appContext)
        {
            _modelProviders ??= appContext.TypeScanner
                    .FindTypes<IODataModelProvider>()
                    .Select(x => (IODataModelProvider)Activator.CreateInstance(x))
                    .ToList();

            return _modelProviders;
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddAuthentication("Smartstore.WebApi.Basic")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Smartstore.WebApi.Basic", null);
            
            services.Configure<MvcOptions>(o =>
            {
                o.RespectBrowserAcceptHeader = true;
                o.Conventions.Add(new ApiControllerModelConvention());
            });

            services
                .AddSwaggerGen(o =>
                {
                    // INFO: "name" equals ApiExplorer.GroupName. Must be globally unique, URI-friendly and should be in lower case.
                    o.SwaggerDoc("webapi1", new OpenApiInfo
                    {
                        Version = "1",
                        Title = "Smartstore Web API"
                    });

                    // INFO: required workaround to avoid conflict errors for identical action names such as "Get" when opening swagger UI.
                    // Alternative: set-up a path template for each (!) OData action method.
                    o.ResolveConflictingActions(descriptions => descriptions.First());

                    //o.IgnoreObsoleteActions();
                    //o.IgnoreObsoleteProperties();

                    // Avoids "Conflicting schemaIds" (multiple types with the same name but different namespaces).
                    o.CustomSchemaIds(type => type.FullName);

                    o.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
                    {
                        Name = HeaderNames.Authorization,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Basic",
                        In = ParameterLocation.Header,
                        Description = "Please enter your public and private API key."
                    });

                    o.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Basic",
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                    // INFO: provide unique OperationId. By default ApiDescription.RelativePath is used but that is not unique.
                    // Prevents multiple descriptions from opening at the same time when clicking a method.
                    o.CustomOperationIds(x => x.HttpMethod.ToLower().Grow(x.RelativePath, "/"));

                    // Ordering within a group does not work. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/401
                    //o.OrderActionsBy(x => ...);

                    // Filters.
                    o.DocumentFilter<SwaggerDocumentFilter>();
                    o.OperationFilter<SwaggerOperationFilter>();

                    // Schema filtering does not work as expected if you only want to reduce the depth of the generated examples
                    // without changing the actual schema definition. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/615
                    //o.SchemaFilter<SwaggerSchemaFilter>();

                    //o.MapType<decimal>(() => new OpenApiSchema
                    //{
                    //    Type = "number($double)",
                    //    Example = new OpenApiDouble(16.5)
                    //});

                    try
                    {
                        // XML comments.
                        var modelProviders = GetModelProviders(appContext);

                        foreach (var provider in modelProviders)
                        {
                            var stream = provider.GetXmlCommentsStream(appContext);
                            if (stream != null)
                            {
                                // INFO: XPathDocument closes the input stream.
                                o.IncludeXmlComments(() => new XPathDocument(stream), true);
                            }
                        }

                        // Concrete example values for entity properties could be defined per XML comment at the entities:
                        //o.IncludeXmlComments(@"...\Smartstore.Full\Smartstore\src\Smartstore.Web\bin\Debug\Smartstore.Core.xml");
                    }
                    catch (Exception ex)
                    {
                        appContext.Logger.Error(ex);
                    }
                });

            // INFO: needs to be placed after AddSwaggerGen(). Without this statement, the examples in the documentation
            // will contain everything, every tiny bit of any related object will be serialized.
            services.AddSwaggerGenNewtonsoftSupport();
        }

        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            //services.TryAddEnumerable(ServiceDescriptor.Transient<IODataControllerActionConvention, CustomRoutingConvention>());

            mvcBuilder
                .AddOData(o =>
                {
                    var modelBuilder = new ODataConventionModelBuilder();
                    var modelProviders = GetModelProviders(appContext);

                    foreach (var provider in modelProviders)
                    {
                        provider.Build(modelBuilder, 1);
                    }

                    var edmModel = modelBuilder.GetEdmModel();
                    var settings = appContext.Services.ResolveOptional<WebApiSettings>() ?? new();

                    o.EnableQueryFeatures(settings.MaxTop);
                    o.AddRouteComponents("odata/v1", edmModel, services =>
                    {
                        // Perf: https://devblogs.microsoft.com/odata/using-the-new-json-writer-in-odata/
                        services.AddSingleton<IStreamBasedJsonWriterFactory>(_ => DefaultStreamBasedJsonWriterFactory.Default);
                        //services.AddSingleton<ODataSerializerProvider>(sp => new MySerializerProvider(sp));

                        var bh = new DefaultODataBatchHandler();
                        bh.MessageQuotas.MaxNestingDepth = settings.MaxBatchNestingDepth;
                        bh.MessageQuotas.MaxOperationsPerChangeset = settings.MaxBatchOperationsPerChangeset;
                        bh.MessageQuotas.MaxReceivedMessageSize = 1024 * settings.MaxBatchReceivedMessageSize;

                        services.AddSingleton<ODataBatchHandler>(_ => bh);
                    });

                    o.TimeZone = TimeZoneInfo.Utc;
                    o.RouteOptions.EnableUnqualifiedOperationCall = true;
                    //o.Conventions.Add(new CustomRoutingConvention());
                });

                // INFO: no effect using OData 8.0.11 and OData.NewtonsoftJson 8.0.4. JSON is never written with Newtonsoft.Json.
                //.AddODataNewtonsoftJson();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<WebApiService>().As<IWebApiService>().InstancePerLifetimeScope();
            builder.RegisterType<ApiUserStore>().As<IApiUserStore>().SingleInstance();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            if (builder.ApplicationContext.IsInstalled)
            {
                var routePrefix = WebApiSettings.SwaggerRoutePrefix;
                var isDev = builder.ApplicationContext.HostEnvironment.IsDevelopment();

                builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
                {
                    app.UseSwagger(o =>
                    {
                        o.RouteTemplate = routePrefix + "/{documentName}/swagger.{json|yaml}";
                    });

                    app.UseSwaggerUI(o =>
                    {
                        o.SwaggerEndpoint($"/{routePrefix}/webapi1/swagger.json", "Web API");
                        o.RoutePrefix = routePrefix;

                        // Only show schemas dropdown for developers.
                        o.DefaultModelsExpandDepth(isDev ? 0 : -1);
                        //o.DefaultModelRendering(ModelRendering.Model);

                        o.EnablePersistAuthorization();
                        //o.EnableTryItOutByDefault();
                        //o.DisplayOperationId();
                        o.DisplayRequestDuration();
                        o.DocExpansion(DocExpansion.List);
                        o.EnableFilter();
                        //o.ShowCommonExtensions();
                        //o.InjectStylesheet("/swagger-ui/custom.css");
                    });
                });

                builder.Configure(StarterOrdering.BeforeRoutingMiddleware, app => 
                {
                    if (isDev)
                    {
                        // Navigate to ~/$odata to determine whether any endpoints did not match an odata route template.
                        app.UseODataRouteDebug();
                    }

                    // Add OData /$query middleware.
                    app.UseODataQueryRequest();

                    // Add the OData Batch middleware to support OData $batch.
                    app.UseODataBatching();
                    app.UseMiddleware<ODataBatchHttpContextAccessor>();

                    // If you want to use /$openapi, enable the middleware.
                    //app.UseODataOpenApi();
                });
            }
        }
    }
}
