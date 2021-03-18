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
        /// Not used by Smartstore.
        /// </summary>
        public const int Early = -500;

        /// <summary>
        /// Default ordering of stock calculators.
        /// </summary>
        public const int Default = 0;

        /// <summary>
        /// Not used by Smartstore.
        /// </summary>
        public const int Late = 500;

        /// <summary>
        /// Not used by Smartstore.
        /// </summary>
        public const int Last = 1000;
    }
}
