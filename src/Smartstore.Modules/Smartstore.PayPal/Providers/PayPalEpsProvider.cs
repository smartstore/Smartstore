using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.Eps)]
    [FriendlyName("PayPal eps-Überweisung")]
    [Order(1)]
    public class PayPalEpsProvider : PayPalApmProviderBase
    {
        public PayPalEpsProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "eps");
    }
}