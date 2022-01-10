using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.AmazonPay.Filters;
using Smartstore.AmazonPay.Services;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddScoped<IAmazonPayService, AmazonPayService>();

            services.AddAuthentication(AmazonPaySignInProvider.SystemName)
                .AddScheme<AmazonPaySignInOptions, AmazonPaySignInHandler>(AmazonPaySignInProvider.SystemName, null);

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<OffCanvasShoppingCartFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("ShoppingCart", nameof(ShoppingCartController.OffCanvasShoppingCart)) ?? false);

                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>() && !context.HttpContext.Request.IsAjaxRequest());
            });
        }
    }
}
