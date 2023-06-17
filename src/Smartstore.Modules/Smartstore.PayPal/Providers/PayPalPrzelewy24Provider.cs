using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.Przelewy24)]
    [FriendlyName("PayPal Przelewy24")]
    [Order(1)]
    public class PayPalPrzelewy24Provider : PayPalApmProviderBase
    {
        public PayPalPrzelewy24Provider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "p24");
    }
}