using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.StripeElements.Filters;
using Smartstore.StripeElements.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.StripeElements
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<OffCanvasShoppingCartFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("ShoppingCart", nameof(ShoppingCartController.OffCanvasShoppingCart)) ?? false);

                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>(x => x.PaymentMethod()) && !context.HttpContext.Request.IsAjax()
                    || context.ControllerIs<CheckoutController>(x => x.Confirm()) && !context.HttpContext.Request.IsAjax(), 200);

                o.Filters.AddConditional<ScriptIncludeFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjax());
            });

            if (appContext.IsInstalled)
            {
                services.AddScoped<StripeHelper>();
            }
        }
    }
}