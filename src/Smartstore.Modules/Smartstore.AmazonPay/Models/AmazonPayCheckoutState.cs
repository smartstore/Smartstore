using Smartstore.ComponentModel;

namespace Smartstore.AmazonPay.Models
{
    [Serializable]
    public class AmazonPayCheckoutState : ObservableObject
    {
        /// <summary>
        /// The identifier of the AmazonPay checkout session object.
        /// </summary>
        public string SessionId
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
        /// Order is confimed by buyer and AmazonPay -> automatically submit confirm form.
        /// </summary>
        public bool SubmitForm
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        #endregion

        public override string ToString()
        {
            return $"SessionId:{SessionId}, IsConfirmed:{IsConfirmed}, SubmitForm:{SubmitForm}";
        }
    }
}
