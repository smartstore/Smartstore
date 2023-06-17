using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.MyBank)]
    [FriendlyName("PayPal MyBank")]
    [Order(1)]
    public class PayPalMyBankProvider : PayPalApmProviderBase
    {
        public PayPalMyBankProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "mybank");
    }
}