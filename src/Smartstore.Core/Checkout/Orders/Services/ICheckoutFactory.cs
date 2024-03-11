#nullable enable

namespace Smartstore.Core.Checkout.Orders
{
    public static partial class CheckoutNames
    {
        public const string Standard = "standard";
        public const string Terminal = "terminal";
    }

    public interface ICheckoutFactory
    {
        /// <summary>
        /// Gets a list of checkout steps to be processed.
        /// </summary>
        CheckoutStep[] GetCheckoutSteps();

        /// <summary>
        /// Gets the checkout step for route values.
        /// </summary>
        CheckoutStep GetCheckoutStep(string action, string controller = "Checkout", string? area = null);

        /// <summary>
        /// Gets the next/previous step in checkout.
        /// </summary>
        /// <param name="step">Step to get the next/previous checkout step for.</param>
        /// <param name="next"><c>true</c> to get the next, <c>false</c> to get the previous checkout step.</param>
        CheckoutStep GetNextCheckoutStep(CheckoutStep step, bool next);
    }
}
