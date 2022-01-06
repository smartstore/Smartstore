using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.AmazonPay.Filters;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Stores;
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
                .AddScheme<AmazonPaySignInOptions, AmazonPaySignInHandler>(AmazonPaySignInProvider.SystemName, options =>
                {
                    var storeContext = appContext.Services.Resolve<IStoreContext>();
                    var httpContext = appContext.Services.Resolve<IHttpContextAccessor>().HttpContext;

                    options.StoreId = storeContext.CurrentStore.Id;
                    options.BuyerToken = httpContext.Session.GetString("AmazonPayBuyerToken");
                });

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<OffCanvasShoppingCartFilter>(
                    context => context.RouteData?.Values?.IsSameRoute("ShoppingCart", nameof(ShoppingCartController.OffCanvasShoppingCart)) ?? false);

                o.Filters.AddConditional<CheckoutFilter>(
                    context => context.ControllerIs<CheckoutController>() && !context.HttpContext.Request.IsAjaxRequest());
            });
        }

        //public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        //{
        //    builder.RegisterType<AmazonPaySignInOptionsConfigurer>()
        //        .As<IConfigureOptions<AuthenticationOptions>>()
        //        .As<IConfigureNamedOptions<AmazonPaySignInOptions>>()
        //        .InstancePerDependency();

        //    builder.RegisterType<PostConfigureOptions<AmazonPaySignInOptions, AmazonPaySignInHandler>>()
        //        .As<IPostConfigureOptions<AmazonPaySignInOptions>>()
        //        .InstancePerDependency();
        //}
    }
}
