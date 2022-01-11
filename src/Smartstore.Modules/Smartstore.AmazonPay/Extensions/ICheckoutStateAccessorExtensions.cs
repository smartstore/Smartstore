using Smartstore.Core.Checkout.Orders;

namespace Smartstore.AmazonPay
{
    internal static class ICheckoutStateAccessorExtensions
    {
        public static AmazonPayCheckoutState GetAmazonPayCheckoutState(this ICheckoutStateAccessor checkoutStateAccessor)
        {
            Guard.NotNull(checkoutStateAccessor, nameof(checkoutStateAccessor));

            var prop = checkoutStateAccessor.CheckoutState.CustomProperties;

            var state = new AmazonPayCheckoutState
            {
                SessionId = prop.Get("AmazonPayCheckout.SessionId") as string,
                FormData = prop.Get("AmazonPayCheckout.FormData") as string
            };

            if (prop.TryGetValue("AmazonPayCheckout.IsConfirmed", out var isConfirmed))
            {
                state.IsConfirmed = (bool)isConfirmed;
            }

            if (prop.TryGetValue("AmazonPayCheckout.SubmitForm", out var submitForm))
            {
                state.SubmitForm = (bool)submitForm;
            }

            return state;
        }

        public static void SetAmazonPayCheckoutState(this ICheckoutStateAccessor checkoutStateAccessor, AmazonPayCheckoutState state)
        {
            Guard.NotNull(checkoutStateAccessor, nameof(checkoutStateAccessor));

            var prop = checkoutStateAccessor.CheckoutState.CustomProperties;

            if (state == null)
            {
                prop.Remove("AmazonPayCheckout.SessionId");
                prop.Remove("AmazonPayCheckout.IsConfirmed");
                prop.Remove("AmazonPayCheckout.FormData");
                prop.Remove("AmazonPayCheckout.SubmitForm");
            }
            else
            {
                prop["AmazonPayCheckout.SessionId"] = state.SessionId;
                prop["AmazonPayCheckout.IsConfirmed"] = state.IsConfirmed;
                prop["AmazonPayCheckout.FormData"] = state.FormData;
                prop["AmazonPayCheckout.SubmitForm"] = state.SubmitForm;
            }
        }
    }
}
