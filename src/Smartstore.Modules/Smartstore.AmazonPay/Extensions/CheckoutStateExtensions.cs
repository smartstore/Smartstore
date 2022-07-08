using Smartstore.Core.Checkout.Orders;

namespace Smartstore.AmazonPay
{
    internal static class CheckoutStateExtensions
    {
        const string CheckoutStateKey = "AmazonPayCheckout";

        public static AmazonPayCheckoutState GetAmazonPayCheckoutState(this CheckoutState checkoutState)
        {
            Guard.NotNull(checkoutState, nameof(checkoutState));

            if (!checkoutState.CustomProperties.TryGetValue(CheckoutStateKey, out var obj) || obj == null)
            {
                obj = new AmazonPayCheckoutState();
                checkoutState.CustomProperties[CheckoutStateKey] = obj;
            }

            return (AmazonPayCheckoutState)obj;
        }

        public static bool RemoveAmazonPayCheckoutState(this CheckoutState checkoutState)
        {
            Guard.NotNull(checkoutState, nameof(checkoutState));

            return checkoutState.CustomProperties.TryRemove(CheckoutStateKey, out _);
        }
    }
}
