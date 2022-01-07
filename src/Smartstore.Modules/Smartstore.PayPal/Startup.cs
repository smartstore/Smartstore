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
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<MiniBasketFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("ShoppingCart", nameof(ShoppingCartController.OffCanvasShoppingCart)) ?? false);
                o.Filters.AddConditional<ScriptIncludeFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest(), 200);
                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>() && !context.HttpContext.Request.IsAjaxRequest(), 200);
            });

            services.AddHttpClient<PayPalHttpClient>()
                .AddSmartstoreUserAgent()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });
        }
    }
}
