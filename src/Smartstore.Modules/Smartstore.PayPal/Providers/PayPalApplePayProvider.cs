using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Providers
{
    /// <summary>
    /// https://developer.paypal.com/docs/checkout/apm/apple-pay/
    /// </summary>
    [SystemName(PayPalConstants.ApplePay)]
    [FriendlyName("PayPal Apple Pay")]
    [Order(2)]
    public class PayPalApplePayProvider : PayPalProviderBase
    {
        public PayPalApplePayProvider(
            SmartDbContext db,
            PayPalHttpClient client,
            PayPalSettings settings,
            IPaymentService paymentService,
            ICheckoutStateAccessor checkoutStateAccessor)
            : base(db, client, settings, paymentService, checkoutStateAccessor)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApplePayViewComponent), true);
    }
}
