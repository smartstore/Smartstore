namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Some predefined defaults for the numerical ordering of price calculators.
    /// </summary>
    public static class CalculatorOrdering
    {
        /// <summary>
        /// Not used by Smartstore.
        /// </summary>
        public const int First = -1000;

        /// <summary>
        /// Used by calculators that should run early in the pipeline.
        /// </summary>
        public const int Early = -500;

        /// <summary>
        /// Default ordering of stock calculators.
        /// </summary>
        public const int Default = 0;

        /// <summary>
        /// Used by calculators that should run late in the pipeline (e.g. used by <see cref="Smartstore.Core.Catalog.Pricing.Calculators.DiscountPriceCalculator"/>.
        /// </summary>
        public const int Late = 500;

        /// <summary>
        /// Not used by Smartstore.
        /// </summary>
        public const int Last = 1000;
    }
}
