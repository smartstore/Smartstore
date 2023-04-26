using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Providers
{
    [SystemName("Payments.PayPalPayLater")]
    [FriendlyName("PayPal Pay Later")]
    [Order(1)]
    public class PayPalPayLaterProvider : PayPalProviderBase
    {
        public PayPalPayLaterProvider(SmartDbContext db, PayPalHttpClient client, PayPalSettings settings)
            : base(db, client, settings)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalPayLaterViewComponent), true);
    }
}