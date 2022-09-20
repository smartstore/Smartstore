using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Api.Security;
using Smartstore.Web.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Smartstore.Web.Api
{
    // TODO: (mg) (core) Enable OData batching via app.UseODataBatching(). See: https://devblogs.microsoft.com/odata/tutorial-creating-a-service-with-odata-8-0/
    // PS: that document looks outdated. MapODataRoute and ODataOptions.AddModel does not exist anymore.

    /// <summary>
    /// For proper configuration see https://github.com/domaindrivendev/Swashbuckle.AspNetCore
    /// </summary>
    internal class Startup : StarterBase
    {
        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            //services.TryAddEnumerable(ServiceDescriptor.Transient<IODataControllerActionConvention, CustomRoutingConvention>());

            mvcBuilder
                .AddOData(o =>
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

                    o.EnableQueryFeatures(WebApiSettings.DefaultMaxTop);
                    o.AddRouteComponents("odata/v1", edmModel);

                    o.TimeZone = TimeZoneInfo.Utc;
                    o.RouteOptions.EnableUnqualifiedOperationCall = true;

                    //o.EnableAttributeRouting = true;
                    //o.Conventions.Add(new CustomRoutingConvention());
                });

            mvcBuilder.Services
                //.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerGenOptionsConfiguration>()
                .AddSwaggerGen(o =>
                {
                    // INFO: "name" equals ApiExplorer.GroupName. Must be globally unique, URI-friendly and should be in lower case.
                    // Smartstore sets the ApiExplorer.GroupName dynamically based on the namespace of the API controller, see SmartApiExplorerConvention.
                    o.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Smartstore Web API"
                    });

                    // INFO: required workaround to avoid conflict errors for identical action names such as "Get" when opening swagger UI.
                    // Alternative: set-up a path template for each (!) OData action method.
                    o.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                    //o.IgnoreObsoleteActions();
                    //o.IgnoreObsoleteProperties();

                    //o.UseAllOfForInheritance();

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

                    // Filters.
                    o.DocumentFilter<SwaggerDocumentFilter>();
                    o.OperationFilter<SwaggerResponseFilter>();

                    // TODO: (mg) (core) add SwaggerOperationFilter.
                    //o.OperationFilter<SwaggerOperationFilter>();

                    try
                    {
                        // INFO: enable "Documentation file" in project properties. Leave file path empty.
                        // Optionally append 1591 to "Suppress specific warnings" to suppress warning about missing XML comments.
                        var fileName = Assembly.GetExecutingAssembly().GetName().Name;
                        var xmlFile = appContext.ModulesRoot.GetFile($"{Module.SystemName}/{fileName}.xml");

                        // TODO: (mg) (core) we probably need an XML doc factory because code comments are spread over several projects.
                        o.IncludeXmlComments(xmlFile.PhysicalPath, true);
                    }
                    catch (Exception ex)
                    {
                        ex.Dump();
                    }
                });

            // We are using Newtonsoft so we have to explicit opt-in. Needs to be placed after AddSwaggerGen().
            mvcBuilder.Services.AddSwaggerGenNewtonsoftSupport();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            if (builder.ApplicationContext.IsInstalled)
            {
                builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
                {
                    // TODO: (mg) (core) Not sure whether this is the correct ordering for Swagger?! Investigate please.
                    app.UseSwagger(options =>
                    {
                    });

                    app.UseSwaggerUI(o =>
                    {
                        o.SwaggerEndpoint("v1/swagger.json", "v1");

                        // Hide schemas dropdown.
                        o.DefaultModelsExpandDepth(-1);
                        //o.EnableTryItOutByDefault();
                        //o.DisplayOperationId();
                        o.DisplayRequestDuration();
                        o.DocExpansion(DocExpansion.List);
                        //o.EnableFilter();
                        //o.ShowCommonExtensions();
                        //o.InjectStylesheet("/swagger-ui/custom.css");
                    });
                });

                builder.Configure(StarterOrdering.BeforeRoutingMiddleware, app => 
                {
                    // Navigate to ~/$odata to determine whether any endpoints did not match an odata route template.
                    if (builder.ApplicationContext.HostEnvironment.IsDevelopment())
                    {
                        app.UseODataRouteDebug();
                    }

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

            services.Configure<MvcOptions>(o => o.Conventions.Add(new ApiExplorerConvention()));
        }
    }
}
