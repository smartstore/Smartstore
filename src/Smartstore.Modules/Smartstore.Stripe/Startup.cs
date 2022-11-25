using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.StripeElements.Filters;
using Smartstore.Web.Controllers;
using Stripe;

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
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // TODO: (mh) (core) Add options configurer
            StripeConfiguration.ApiKey = "sk_test_51M3yXmB6RUqeW6sBeU4Ari5jNssIA2gVFVXvxqKuOzKKQzYGt2EbLn9mxRRUovXI9hDcDuSjzukyoN1FMpZGnNnp001vAIgVkx";
        }
    }
}