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
                o.Filters.AddEndpointFilter<OffCanvasShoppingCartFilter, SmartController>()
                    .ForController("ShoppingCart")
                    .ForAction(nameof(ShoppingCartController.OffCanvasShoppingCart));
                o.Filters.AddEndpointFilter<StripeScriptIncludeFilter, PublicController>().WhenNonAjax();
                o.Filters.AddEndpointFilter<CheckoutFilter, CheckoutController>(order: 200)
                    .ForAction(x => x.PaymentMethod())
                    .ForAction(x => x.Confirm())
                    .WhenNonAjax();
            });

            if (appContext.IsInstalled)
            {
                services.AddScoped<StripeHelper>();
            }
        }
    }
}