using System;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Tests.Checkout.Payment
{
    [PaymentMethodType(PaymentMethodType = PaymentMethodType.Standard)]
    public class TestPaymentMethod1 : PaymentMethodBase
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
            => true;

        public override bool SupportPartiallyRefund
            => true;

        public override bool SupportRefund
            => true;

        public override bool SupportVoid
            => true;

        public override Widget GetPaymentInfoWidget()
            => throw new NotImplementedException();
    }
}
