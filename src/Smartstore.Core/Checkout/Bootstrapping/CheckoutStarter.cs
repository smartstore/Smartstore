using Autofac;
using Smartstore.Core.Checkout.Affiliates.Rules;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Handlers;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Payment.Rules;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Shipping.Rules;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Builders;
using Smartstore.Net;
using Smartstore.Net.Http;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class CheckoutStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddHttpClient<PdfInvoiceHttpClient>()
                .AddSmartstoreUserAgent()
                .PropagateCookies(CookieNames.Identity, CookieNames.Visitor)
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                });

            services.AddHttpClient<ViesTaxationHttpClient>()
                .AddSmartstoreUserAgent()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<CheckoutAttributeMaterializer>().As<ICheckoutAttributeMaterializer>().InstancePerLifetimeScope();
            builder.RegisterType<CheckoutAttributeFormatter>().As<ICheckoutAttributeFormatter>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultCheckoutStateAccessor>().As<ICheckoutStateAccessor>().InstancePerLifetimeScope();
            builder.RegisterType<OrderCalculationService>().As<IOrderCalculationService>().InstancePerLifetimeScope();
            builder.RegisterType<OrderProcessingService>().As<IOrderProcessingService>().InstancePerLifetimeScope();
            builder.RegisterType<ShoppingCartValidator>().As<IShoppingCartValidator>().InstancePerLifetimeScope();
            builder.RegisterType<ShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
            builder.RegisterType<GiftCardService>().As<IGiftCardService>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingService>().As<IShippingService>().InstancePerLifetimeScope();
            builder.RegisterType<PaymentService>().As<IPaymentService>().InstancePerLifetimeScope();
            builder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
            builder.RegisterType<TaxService>().As<ITaxService>().InstancePerLifetimeScope();
            builder.RegisterType<TaxCalculator>().As<ITaxCalculator>().InstancePerLifetimeScope();

            // Checkout
            builder.RegisterType<CheckoutFactory>().As<ICheckoutFactory>().InstancePerLifetimeScope();
            builder.RegisterType<CheckoutWorkflow>().As<ICheckoutWorkflow>().InstancePerLifetimeScope();
            DiscoverCheckoutHandlers(builder, appContext);

            // Cart rules.
            var cartRuleTypes = appContext.TypeScanner.FindTypes<IRule<CartRuleContext>>().ToList();
            foreach (var ruleType in cartRuleTypes)
            {
                builder.RegisterType(ruleType).Keyed<IRule<CartRuleContext>>(ruleType).InstancePerLifetimeScope();
            }

            builder.RegisterType<CartRuleProvider>().As<ICartRuleProvider>().InstancePerLifetimeScope();

            // Rule options provider.
            builder.RegisterType<PaymentMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<AuthenticationMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingRateComputationMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<AffiliateRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }

        private static void DiscoverCheckoutHandlers(ContainerBuilder builder, IApplicationContext appContext)
        {
            var handlerTypes = appContext.TypeScanner.FindTypes<ICheckoutHandler>();

            foreach (var handlerType in handlerTypes)
            {
                var targetAttribute = handlerType.GetAttribute<CheckoutStepAttribute>(true);

                builder
                    .RegisterType(handlerType)
                    .As<ICheckoutHandler>()
                    .Keyed<ICheckoutHandler>(handlerType)
                    .InstancePerAttributedLifetime()
                    .WithMetadata<CheckoutHandlerMetadata>(m =>
                    {
                        m.For(x => x.HandlerType, handlerType);
                        m.For(x => x.Actions, targetAttribute?.Actions);
                        m.For(x => x.Controller, targetAttribute?.Controller ?? "Checkout");
                        m.For(x => x.Area, targetAttribute?.Area);
                        m.For(x => x.Order, targetAttribute?.Order ?? 0);
                        m.For(x => x.ProgressLabelKey, targetAttribute?.ProgressLabelKey);
                    });
            }

            builder.Register<Func<Type, ICheckoutHandler>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return key => cc.ResolveKeyed<ICheckoutHandler>(key);
            });
        }
    }
}
