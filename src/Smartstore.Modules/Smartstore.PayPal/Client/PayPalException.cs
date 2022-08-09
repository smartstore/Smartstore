using Smartstore.Core.Checkout.Payment;

namespace Smartstore.PayPal.Client
{
    public class PayPalException : PaymentException
    {
        const string ProviderName = "PayPal";

        public PayPalException(string message, PayPalResponse response)
            : base(message, response, ProviderName)
        {
        }

        public PayPalException(string message)
            : base(message, ProviderName)
        {
        }

        public override PayPalResponse Response
            => (PayPalResponse)base.Response;
    }
}
