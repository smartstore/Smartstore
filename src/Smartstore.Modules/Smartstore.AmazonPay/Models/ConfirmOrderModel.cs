namespace Smartstore.AmazonPay.Models
{
    public class ConfirmOrderModel : AmazonPayButtonModel
    {
        public ConfirmOrderModel(AmazonPaySettings settings, AmazonPayCheckoutState state)
            : base(settings)
        {
            Guard.NotNull(state, nameof(state));

            CheckoutSessionId = state.CheckoutSessionId;
            IsConfirmed = state.IsConfirmed;
            FormData = state.FormData;
            SubmitForm = state.SubmitForm;
        }

        public string CheckoutSessionId { get; }
        public bool IsConfirmed { get; }
        public string FormData { get; }
        public bool SubmitForm { get; }
    }
}
