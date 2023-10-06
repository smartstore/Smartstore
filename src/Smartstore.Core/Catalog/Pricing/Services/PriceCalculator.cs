using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    public delegate Task CalculatorDelegate(CalculatorContext context);

    public interface IPriceCalculator
    {
        Task CalculateAsync(CalculatorContext context, CalculatorDelegate next);
    }

    public abstract class PriceCalculator : IPriceCalculator
    {
        private readonly IPriceCalculatorFactory _calculatorFactory;

        protected PriceCalculator(IPriceCalculatorFactory calculatorFactory)
        {
            _calculatorFactory = calculatorFactory;
        }

        public virtual async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            Calculate(context);
            await next(context);
        }

        protected virtual void Calculate(CalculatorContext context)
        {
        }

        /// <summary>
        /// Gets the final price for a child product by running a nested calculation pipeline.
        /// </summary>
        /// <param name="childProduct">
        /// The child product (e.g. associated product of a grouped product or a bundle item part) to calculate price for.
        /// Must not be the same product as <see cref="PriceCalculationContext.Product"/>.
        /// </param>
        /// <param name="context">The calculator context of the root pipeline.</param>
        /// <param name="childContextConfigurer">An optional configurer action for the child context.</param>
        /// <returns>The nested calculator context.</returns>
        protected async Task<CalculatorContext> CalculateChildPriceAsync(Product childProduct, CalculatorContext context, Action<CalculatorContext> childContextConfigurer = null)
        {
            if (context.Product == childProduct)
            {
                throw new InvalidOperationException("The product of a nested calculation pipeline cannot be the same as that of the root pipeline. It would result in a deadlock."
                    + $" Please fix product with ID {context.Product.Id}.");
            }

            var childCalculatorContext = new CalculatorContext(context, childProduct.Price)
            {
                Product = childProduct,
                Options = context.Options.Clone()
            };

            childContextConfigurer?.Invoke(childCalculatorContext);

            if (context.Options.ChildProductsBatchContext != null)
            {
                childCalculatorContext.Options.BatchContext = context.Options.ChildProductsBatchContext;
            }

            // Get calculators for child product context
            var calculators = _calculatorFactory.GetCalculators(childCalculatorContext);

            // Run calculators
            await _calculatorFactory.RunCalculators(calculators, childCalculatorContext);

            return childCalculatorContext;
        }
    }
}
