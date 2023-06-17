using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.Bancontact)]
    [FriendlyName("PayPal Bancontact")]
    [Order(1)]
    public class PayPalBancontactProvider : PayPalApmProviderBase
    {
        public PayPalBancontactProvider(PayPalApmServiceContext context) : base(context)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent), "bancontact");
    }
}