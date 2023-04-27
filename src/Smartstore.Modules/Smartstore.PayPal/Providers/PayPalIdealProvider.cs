using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName("Payments.PayPalIdeal")]
    [FriendlyName("PayPal iDEAL")]
    [Order(1)]
    public class PayPalIdealProvider : PayPalApmProviderBase
    {
        public PayPalIdealProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "ideal");

        // TODO: (mh) (core) Add BIC Input field
    }
}