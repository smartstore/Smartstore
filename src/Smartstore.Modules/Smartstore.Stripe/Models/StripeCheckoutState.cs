using Smartstore.ComponentModel;
using Stripe;

namespace Smartstore.StripeElements.Models
{
    [Serializable]
    public class StripeCheckoutState : ObservableObject
    {
        public PaymentIntent PaymentIntent
        {
            get => GetProperty<PaymentIntent>();
            set => SetProperty(value);
        }

        public bool ButtonUsed
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        public string PaymentMethod
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public override string ToString()
        {
            return $"PaymentIntentId:{PaymentIntent.Id}";
        }
    }
}
