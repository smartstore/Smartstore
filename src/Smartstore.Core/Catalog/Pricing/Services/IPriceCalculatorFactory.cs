namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Responsible for resolving price calculators for a given context,
    /// and for running them.
    /// </summary>
    public partial interface IPriceCalculatorFactory
    {
        /// <summary>
        /// Resolves all calculators for the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The context to resolve calculators for</param>
        /// <returns>An array of calculators which represent the calculation pipeline.</returns>
        IPriceCalculator[] GetCalculators(PriceCalculationContext context);

        /// <summary>
        /// Runs a calculation pipeline by invoking passed <paramref name="calculators"/>.
        /// </summary>
        /// <param name="calculators">The calculators to invoke.</param>
        /// <param name="context">The calculator context that is passed through the pipeline.</param>
        Task RunCalculators(IPriceCalculator[] calculators, CalculatorContext context);
    }
}
