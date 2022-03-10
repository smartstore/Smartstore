using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Tests.Checkout.Payment
{
    public class TestPaymentMethod2 : PaymentMethodBase
    {
        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Paid
            };

            return Task.FromResult(result);
        }

        public override bool SupportCapture 
            => false;

        public override bool SupportPartiallyRefund 
            => false;

        public override bool SupportRefund 
            => false;

        public override bool SupportVoid 
            => false;

        public decimal GetAdditionalHandlingFee(IList<OrganizedShoppingCartItem> cart)
            => decimal.Zero;

        public override WidgetInvoker GetPaymentInfoWidget()
            => throw new NotImplementedException();

        public override PaymentMethodType PaymentMethodType 
            => PaymentMethodType.Standard;
    }
}
