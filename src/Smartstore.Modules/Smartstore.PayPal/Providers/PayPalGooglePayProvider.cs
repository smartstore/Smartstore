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
    /// https://developer.paypal.com/docs/checkout/apm/google-pay/
    /// </summary>
    [SystemName(PayPalConstants.GooglePay)]
    [FriendlyName("PayPal Google Pay")]
    [Order(1)]
    public class PayPalGooglePayProvider : PayPalProviderBase
    {
        public PayPalGooglePayProvider(
            SmartDbContext db, 
            PayPalHttpClient client, 
            PayPalSettings settings, 
            IPaymentService paymentService, 
            ICheckoutStateAccessor checkoutStateAccessor)
            : base(db, client, settings, paymentService, checkoutStateAccessor)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalGooglePayViewComponent), true);
    }
}