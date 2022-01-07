namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Specifies product types that a price calculator can handle.
    /// </summary>
    [Flags]
    public enum CalculatorTargets
    {
        /// <summary>
        /// A calculator can handle simple products.
        /// </summary>
        Product = 1,

        /// <summary>
        /// A calculator can handle grouped products.
        /// </summary>
        GroupedProduct = 2,

        /// <summary>
        /// A calculator can handle bundle products.
        /// </summary>
        Bundle = 4,

        /// <summary>
        /// A calculator can handle any product type.
        /// </summary>
        All = Product | GroupedProduct | Bundle
    }

    /// <summary>
    /// Specifies the usage of a price calculator. Only classes that implement
    /// <see cref="IPriceCalculator"/> can be annoated with this attribue.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CalculatorUsageAttribute : Attribute
    {
        public CalculatorUsageAttribute(CalculatorTargets validTargets, int order)
        {
            ValidTargets = validTargets;
            Order = order;
        }

        /// <summary>
        /// The valid targets for the annotated calculator. The calculator will be excluded from the pipeline
        /// if the product for which a price is calculated does not match.
        /// </summary>
        public CalculatorTargets ValidTargets { get; }

        /// <summary>
        /// The order of the calculator within the pipeline. See <see cref="CalculatorOrdering"/>.
        /// </summary>
        public int Order { get; }
    }
}
