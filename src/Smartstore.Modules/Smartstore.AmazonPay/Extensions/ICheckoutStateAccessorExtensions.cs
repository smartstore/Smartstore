using Smartstore.Core.Checkout.Orders;

namespace Smartstore.AmazonPay
{
    internal static class ICheckoutStateAccessorExtensions
    {
        public static AmazonPayCheckoutState GetAmazonPayCheckoutState(this ICheckoutStateAccessor checkoutStateAccessor)
        {
            Guard.NotNull(checkoutStateAccessor, nameof(checkoutStateAccessor));

            var props = checkoutStateAccessor.CheckoutState.CustomProperties;

            var state = new AmazonPayCheckoutState
            {
                SessionId = props.Get("AmazonPayCheckout.SessionId") as string,
                FormData = props.Get("AmazonPayCheckout.FormData") as string
            };

            if (props.TryGetValue("AmazonPayCheckout.IsConfirmed", out var isConfirmed))
            {
                state.IsConfirmed = (bool)isConfirmed;
            }

            if (props.TryGetValue("AmazonPayCheckout.SubmitForm", out var submitForm))
            {
                state.SubmitForm = (bool)submitForm;
            }

            return state;
        }

        public static void SetAmazonPayCheckoutState(this ICheckoutStateAccessor checkoutStateAccessor, AmazonPayCheckoutState state)
        {
            Guard.NotNull(checkoutStateAccessor, nameof(checkoutStateAccessor));

            var props = checkoutStateAccessor.CheckoutState.CustomProperties;

            if (state == null)
            {
                props.Remove("AmazonPayCheckout.SessionId");
                props.Remove("AmazonPayCheckout.IsConfirmed");
                props.Remove("AmazonPayCheckout.FormData");
                props.Remove("AmazonPayCheckout.SubmitForm");
            }
            else
            {
                props["AmazonPayCheckout.SessionId"] = state.SessionId;
                props["AmazonPayCheckout.IsConfirmed"] = state.IsConfirmed;
                props["AmazonPayCheckout.FormData"] = state.FormData;
                props["AmazonPayCheckout.SubmitForm"] = state.SubmitForm;
            }
        }
    }
}
