using Smartstore.ComponentModel;

namespace Smartstore.PayPal.Models
{
    [Serializable]
    public class PayPalCheckoutState : ObservableObject
    {
        /// <summary>
        /// Holds the order id returned by PayPal when creating an order at PayPal.
        /// </summary>
        public string PayPalOrderId
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the redirect URL for the APM (alternative payment method) payment flow.
        /// </summary>
        public string ApmRedirectActionUrl
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the full name of the customer. Required for the APM (alternative payment method) payment flow.
        /// </summary>
        public string ApmFullname
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the country ISO-Code (e.g. DE) of the customer. Required for the APM (alternative payment method) payment flow.
        /// </summary>
        public string ApmCountryCode
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the systemname of Smartstore payment provider.
        /// </summary>
        public string ApmProviderSystemName
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the email of the customer. Required for Przelewy24 & BLIK.
        /// </summary>
        public string ApmEmail
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Holds the BIC of the customer. Required for iDEAL.
        /// </summary>
        public string ApmBIC
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
        /// Order is confimed by buyer and APM -> automatically submit confirm form.
        /// </summary>
        public bool SubmitForm
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        #endregion

        public override string ToString()
        {
            return $"TransactionId:{PayPalOrderId}, IsConfirmed:{IsConfirmed}, SubmitForm:{SubmitForm}";
        }
    }
}
