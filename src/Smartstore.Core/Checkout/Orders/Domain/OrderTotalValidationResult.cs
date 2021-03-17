namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents the result of an order total validation.
    /// </summary>
    public partial class OrderTotalValidationResult
    {
        /// <summary>
        /// Minimum allowed order total.
        /// </summary>
        public decimal OrderTotalMinimum { get; init; }

        /// <summary>
        /// Maximum allowed order total.
        /// </summary>
        public decimal OrderTotalMaximum { get; init; }

        /// <summary>
        /// A value indicating whether the order total is above the specified minimum amount.
        /// </summary>
        public bool IsAboveMinimum { get; init; } = true;

        /// <summary>
        /// A value indicating whether the order total is below the specified maximum amount.
        /// </summary>
        public bool IsBelowMaximum { get; init; } = true;

        /// <summary>
        /// A value indicating whether the order total is valid.
        /// </summary>
        public bool IsValid
            => IsAboveMinimum && IsBelowMaximum;
    }
}
