using Autofac;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData;
using Microsoft.OData.Json;
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

                var modelBuilder = new ODataConventionModelBuilder();

                foreach (var provider in modelProviders)
                {
                    provider.Build(modelBuilder, 1);
                }

                var edmModel = modelBuilder.GetEdmModel();

                options.TimeZone = TimeZoneInfo.Utc;

                // Allow OData actions and functions without the need for namespaces (OData V3 backward compatibility).
                // A namespace URL world be for example: /Products(123)/ProductService.FinalPrice
                // Note: the dot in this URL will cause IIS to return error 404. See ExtensionlessUrlHandler-Integrated-4.0.
                options.RouteOptions.EnableUnqualifiedOperationCall = true;

                // Why enabling EnableKeyAsSegment? We need KeyInParenthesis for OData functions anyway.
                options.RouteOptions.EnableKeyAsSegment = false;
                options.RouteOptions.EnableKeyInParenthesis = true;
                options.RouteOptions.EnablePropertyNameCaseInsensitive = true;              

                // INFO: does not have to be set here on the basis of the settings. Will be applied later in ApiQueryableAttribute.
                options.EnableQueryFeatures(WebApiSettings.DefaultMaxTop);
                //options.Conventions.Add(new CustomRoutingConvention());

                options.AddRouteComponents("odata/v1", edmModel, services =>
                {
                    // Perf: https://devblogs.microsoft.com/odata/using-the-new-json-writer-in-odata/
                    services.AddSingleton<IStreamBasedJsonWriterFactory>(_ => DefaultStreamBasedJsonWriterFactory.Default);
                    //services.AddSingleton<ODataSerializerProvider>(sp => new MySerializerProvider(sp));

                    var batchHandler = new DefaultODataBatchHandler();
                    ApplySettings(batchHandler, settings);

                    services.AddSingleton<ODataBatchHandler>(_ => batchHandler);
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
