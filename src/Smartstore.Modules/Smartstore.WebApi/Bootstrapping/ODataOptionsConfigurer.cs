using Autofac;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Smartstore.Engine;

namespace Smartstore.Web.Api.Bootstrapping
{
    internal class ODataOptionsConfigurer : IConfigureOptions<ODataOptions>
    {
        private readonly IApplicationContext _appContext;
        private ODataOptions _prevOptions;

        public ODataOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(ODataOptions options)
        {
            // Resolve required. Do not get via ctor.
            var settings = _appContext.Services.Resolve<WebApiSettings>();

            if (_prevOptions == null)
            {
                var modelProviders = _appContext.TypeScanner
                    .FindTypes<IODataModelProvider>()
                    .Select(x => (IODataModelProvider)Activator.CreateInstance(x))
                    .ToList();

                // TODO: (mg) (core) ODataModelBuilder always reverts back to 'Default' namespace if we choose to set it to null\empty here.
                // As a result, Microsoft.OData.Client generates wrong code with wrong URLs including 'Default' for action methods.
                // See https://github.com/smartstore/Smartstore/issues/389.
                var modelBuilder = new ODataConventionModelBuilder();

                foreach (var provider in modelProviders)
                {
                    provider.Build(modelBuilder, 1);
                }

                var edmModel = modelBuilder.GetEdmModel();

                options.TimeZone = TimeZoneInfo.Utc;

                // Allow OData actions and functions without the need for namespaces (OData V3 backward compatibility).
                // A namespace URL would be for example: /Products(123)/ProductService.CalculatePrice
                // Note: the dot in this URL will cause IIS to return error 404. See ExtensionlessUrlHandler-Integrated-4.0.
                options.RouteOptions.EnableUnqualifiedOperationCall = true;

                // Do not create (duplicate) routes with namespaces such as "/odata/v1/Products({key})/Default.CreateAttributeCombinations".
                options.RouteOptions.EnableQualifiedOperationCall = false;

                // Why enabling EnableKeyAsSegment? We need KeyInParenthesis for OData functions anyway.
                options.RouteOptions.EnableKeyAsSegment = false;
                options.RouteOptions.EnableKeyInParenthesis = true;
                options.RouteOptions.EnablePropertyNameCaseInsensitive = true;              

                // INFO: does not have to be set here on the basis of the settings. Will be applied later in ApiQueryableAttribute.
                options.EnableQueryFeatures(WebApiSettings.DefaultMaxTop);
                //options.Conventions.Add(new CustomRoutingConvention());

                // INFO: we cannot use RouteAttribute on endpoint controllers, e.g. Route("Manufacturers").
                // Would lead to valid, duplicate routes like "/Manufacturers" and "/odata/v1/Manufacturers".
                // We also cannot use Route("odata/v1/Manufacturers"). It produces invalid OData path templates (empty URL segments).

                // INFO: multiple GET endpoints require a route template to avoid AmbiguousMatchException.
                // See also https://github.com/OData/AspNetCoreOData/issues/428
                options.AddRouteComponents("odata/v1", edmModel, services =>
                {
                    //services.AddSingleton<ODataSerializerProvider>(sp => new MySerializerProvider(sp));

                    services.AddSingleton<ODataBatchHandler>(_ =>
                    {
                        var batchHandler = new DefaultODataBatchHandler();
                        ApplySettings(batchHandler, settings);
                        return batchHandler;
                    });
                });
            }
            else
            {
                var routeServices = options.GetRouteServices("odata/v1");
                var batchHandler = routeServices.GetRequiredService<ODataBatchHandler>();
                ApplySettings(batchHandler, settings);
            }

            _prevOptions = options;
        }

        private static void ApplySettings(ODataBatchHandler handler, WebApiSettings settings)
        {
            handler.MessageQuotas.MaxNestingDepth = settings.MaxBatchNestingDepth;
            handler.MessageQuotas.MaxOperationsPerChangeset = settings.MaxBatchOperationsPerChangeset;
            handler.MessageQuotas.MaxReceivedMessageSize = 1024 * settings.MaxBatchReceivedMessageSize;
            //$"apply batch settings MaxNestingDepth:{settings.MaxBatchNestingDepth} MaxOperationsPerChangeset:{settings.MaxBatchOperationsPerChangeset} MaxReceivedMessageSize:{settings.MaxBatchReceivedMessageSize}".Dump();
        }
    }
}
