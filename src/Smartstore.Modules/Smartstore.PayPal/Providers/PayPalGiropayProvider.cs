using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.Giropay)]
    [FriendlyName("PayPal Giropay")]
    [Order(1)]
    public class PayPalGiropayProvider : PayPalApmProviderBase
    {
        public PayPalGiropayProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "giropay");
    }
}