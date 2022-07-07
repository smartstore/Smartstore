using Smartstore.Collections;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutState : ObservableObject
    {
        public static string CheckoutStateSessionKey => ".Smart.CheckoutState";

        /// <summary>
        /// The payment summary as displayed on the checkout confirmation page
        /// </summary>
        public string PaymentSummary
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Indicates whether the payment method selection page was skipped
        /// </summary>
        public bool IsPaymentSelectionSkipped
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Use this dictionary for any custom data required along checkout flow
        /// </summary>
        public ObservableDictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// The payment data entered on payment method selection page
        /// </summary>
        public ObservableDictionary<string, object> PaymentData { get; set; } = new();
    }
}