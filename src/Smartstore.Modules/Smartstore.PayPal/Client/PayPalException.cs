using Smartstore.Core.Checkout.Payment;

namespace Smartstore.PayPal.Client
{
    public class PayPalException : PaymentException
    {
        const string PayPal = "PayPal";
        
        public PayPalException(string message, PayPalResponse response)
            : base(message, response, PayPal)
        {
        }

        public override PayPalResponse Response
            => (PayPalResponse)base.Response;
    }
}
