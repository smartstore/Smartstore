using Amazon.Pay.API;
using Amazon.Pay.API.WebStore;
using Amazon.Pay.API.WebStore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.AmazonPay.Filters;
using Smartstore.AmazonPay.Services;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;
using AmazonPayTypes = Amazon.Pay.API.Types;

namespace Smartstore.AmazonPay
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddScoped<IAmazonPayService, AmazonPayService>();

            services.AddScoped<IWebStoreClient, WebStoreClient>(c =>
            {
                var settings = c.GetRequiredService<AmazonPaySettings>();

                var region = settings.Marketplace.EmptyNull().ToLower() switch
                {
                    "us" => AmazonPayTypes.Region.UnitedStates,
                    "jp" => AmazonPayTypes.Region.Japan,
                    _ => AmazonPayTypes.Region.Europe,
                };

                var config = new ApiConfiguration(
                    region,
                    settings.UseSandbox ? AmazonPayTypes.Environment.Sandbox : AmazonPayTypes.Environment.Live,
                    settings.PublicKeyId,
                    settings.PrivateKey
                );

                var client = new WebStoreClient(config);
                return client;
            });

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<OffCanvasShoppingCartFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("ShoppingCart", nameof(ShoppingCartController.OffCanvasShoppingCart)) ?? false);
            });
        }
    }
}
