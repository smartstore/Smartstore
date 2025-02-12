using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.Trustly)]
    [FriendlyName("PayPal Trustly")]
    [Order(1)]
    public class PayPalTrustlyProvider : PayPalApmProviderBase
    {
        public PayPalTrustlyProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "trustly");
    }
}