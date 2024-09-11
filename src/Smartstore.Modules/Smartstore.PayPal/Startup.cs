using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.OutputCache;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net.Http;
using Smartstore.PayPal.Client;
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
                o.Filters.AddConditional<OffCanvasShoppingCartFilter>(
                    context => context.ControllerIs<ShoppingCartController>(x => x.OffCanvasShoppingCart()));
                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>(x => x.PaymentMethod()) && !context.HttpContext.Request.IsAjax() 
                    || context.ControllerIs<CheckoutController>(x => x.Confirm()) && !context.HttpContext.Request.IsAjax()
                    || context.ControllerIs<CheckoutController>(x => x.BillingAddress()) && !context.HttpContext.Request.IsAjax(), 200);

                o.Filters.AddConditional<PayPalScriptIncludeFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjax());

                o.Filters.AddConditional<ProductDetailFilter>(
                    context => context.ControllerIs<ProductController>(x => x.ProductDetails(0, null)));
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