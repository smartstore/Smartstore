using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net.Http;
using Smartstore.PayPal.Filters;
using Smartstore.PayPal.Services;
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
                o.Filters.AddConditional<MiniBasketFilter>(
                    context => context.ControllerIs<ShoppingCartController>(x => x.OffCanvasShoppingCart()));
                o.Filters.AddConditional<ScriptIncludeFilter>(
                    context => context.ControllerIs(controllerContext => 
                    {
                        if (!controllerContext.HttpContext.Request.IsAjaxRequest())
                        {
                            var descriptor = controllerContext.ActionDescriptor;
                            var controllerType = descriptor.ControllerTypeInfo.AsType();

                            if (controllerType == typeof(ShoppingCartController))
                            {
                                return descriptor.ActionName == "Cart";
                            }
                            else if (controllerType == typeof(CheckoutController))
                            {
                                return descriptor.ActionName is ("Confirm" or "PaymentMethod");
                            }
                        }

                        return false;
                    }), 200);
                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>(x => x.PaymentMethod()) && !context.HttpContext.Request.IsAjaxRequest(), 200);
            });

            // TODO: (mh) (core) Add GZip capability with .AddHttpMessageHandler() or .ConfigurePrimaryHttpMessageHandler(). TBD.
            services.AddHttpClient<PayPalHttpClient>()
                .AddSmartstoreUserAgent()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });
        }
    }
}
