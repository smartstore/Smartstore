using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.PayLater)]
    [FriendlyName("PayPal Pay Later")]
    [Order(1)]
    public class PayPalPayLaterProvider : PayPalProviderBase
    {
        public PayPalPayLaterProvider(
            SmartDbContext db, 
            PayPalHttpClient client, 
            PayPalSettings settings, 
            IPaymentService paymentService, 
            ICheckoutStateAccessor checkoutStateAccessor)
            : base(db, client, settings, paymentService, checkoutStateAccessor)
        {
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalPayLaterViewComponent), true);
    }
}