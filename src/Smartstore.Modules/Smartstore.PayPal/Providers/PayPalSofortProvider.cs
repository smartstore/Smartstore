using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName("Payments.PayPalSofort")]
    [FriendlyName("PayPal Sofort")]
    [Order(1)]
    public class PayPalSofortProvider : PayPalApmProviderBase
    {
        public PayPalSofortProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "sofort");
    }
}