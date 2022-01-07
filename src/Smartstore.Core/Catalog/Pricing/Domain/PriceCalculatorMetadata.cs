namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Metadata for a <see cref="IPriceCalculator"/> implementation.
    /// </summary>
    public sealed class PriceCalculatorMetadata
    {
        /// <summary>
        /// The type of the concrete calculator implementation.
        /// </summary>
        public Type CalculatorType { get; set; }

        /// <summary>
        /// The valid targets for the calculator. The calculator will be excluded from the pipeline
        /// if the product for which a price is calculated does not match.
        /// </summary>
        public CalculatorTargets ValidTargets { get; set; }

        /// <summary>
        /// The order of the calculator within the pipeline. See <see cref="CalculatorOrdering"/>.
        /// </summary>
        public int Order { get; set; }
    }
}
