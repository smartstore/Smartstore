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

        #region Confirmation flow

        public bool IsConfirmed
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        public string FormData
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Order is confimed by buyer and Stripe -> automatically submit confirm form.
        /// </summary>
        public bool SubmitForm
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        #endregion

        public override string ToString()
        {
            return $"PaymentIntentId:{PaymentIntent.Id}";
        }
    }
}
