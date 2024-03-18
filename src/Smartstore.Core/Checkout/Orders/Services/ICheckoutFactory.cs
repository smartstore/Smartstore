#nullable enable

namespace Smartstore.Core.Checkout.Orders
{
    public static partial class CheckoutProcess
    {
        public const string Standard = "Standard";
        public const string Terminal = "Terminal";
        public const string TerminalWithPayment = "Terminal.PaymentMethod";
    }

    public static partial class CheckoutActionNames
    {
        public const string BillingAddress = "BillingAddress";
        public const string SelectBillingAddress = "SelectBillingAddress";
        public const string ShippingAddress = "ShippingAddress";
        public const string SelectShippingAddress = "SelectShippingAddress";
        public const string ShippingMethod = "ShippingMethod";
        public const string PaymentMethod = "PaymentMethod";
        public const string Confirm = "Confirm";
        public const string Completed = "Completed";
    }

    public interface ICheckoutFactory
    {
        /// <summary>
        /// Gets checkout requirements.
        /// </summary>
        CheckoutRequirements GetRequirements();

        /// <summary>
        /// Gets a list of checkout steps to be processed.
        /// </summary>
        CheckoutStep[] GetCheckoutSteps();

        /// <summary>
        /// Gets the checkout step for route values.
        /// </summary>
        /// <param name="action">Name of the action method.</param>
        /// <param name="controller">Name of the controller.</param>
        /// <param name="area">Area name. <c>null</c> by default.</param>
        CheckoutStep? GetCheckoutStep(string action, string controller = "Checkout", string? area = null);

        /// <summary>
        /// Gets the next/previous step in checkout.
        /// </summary>
        /// <param name="step">Step to get the next/previous checkout step for.</param>
        /// <param name="next"><c>true</c> to get the next, <c>false</c> to get the previous checkout step.</param>
        CheckoutStep? GetNextCheckoutStep(CheckoutStep? step, bool next);
    }
}
