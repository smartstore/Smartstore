using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.OutputCache;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net.Http;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Filters;
using Smartstore.PayPal.Services;
using Smartstore.Web.Bundling;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal
{
    internal class Startup : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddEndpointFilter<OffCanvasShoppingCartFilter, ShoppingCartController>()
                    .ForAction(x => x.OffCanvasShoppingCart());
                o.Filters.AddEndpointFilter<PayPalScriptIncludeFilter, PublicController>().WhenNonAjax();
                o.Filters.AddEndpointFilter<ProductDetailFilter, ProductController>()
                    .ForAction(x => x.ProductDetails(0, null));
                o.Filters.AddEndpointFilter<CheckoutFilter, CheckoutController>(order: 200)
                    .ForAction(x => x.PaymentMethod())
                    .ForAction(x => x.Confirm())
                    .ForAction(x => x.BillingAddress())
                    .WhenNonAjax();
            });

            services.AddHttpClient<PayPalHttpClient>()
                .AddSmartstoreUserAgent()
                .ConfigurePrimaryHttpMessageHandler(c => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                })
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            services.AddScoped<PayPalHelper>();
            services.AddScoped<PayPalApmServiceContext>();
            services.AddScoped<PayPalRequestFactory>();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            // OutputCache invalidation configuration
            var observer = builder.ApplicationBuilder.ApplicationServices.GetRequiredService<IOutputCacheInvalidationObserver>();

            observer.ObserveSettingProperty<PayPalSettings>(
                x => x.DisplayProductDetailPayLaterWidget, 
                p => p.InvalidateByRouteAsync(OutputCacheDefaults.ProductDetailsRoute));

            // INFO: We can load the utility js regardless of user consent. It doesn't set any cookies.
            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                // (perf PageSpeed) Get the frontend script bundle and include the utility script
                var bundleCollection = builder.ApplicationBuilder.ApplicationServices.GetRequiredService<IBundleCollection>();
                var bundle = bundleCollection.GetBundleFor("/bundle/js/site.js");
                bundle?.Include("/Modules/Smartstore.PayPal/js/paypal.utils.js");
            });
        }
    }
}