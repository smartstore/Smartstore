using System;
using System.Threading.Tasks;
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
        /// <returns>The nested calculator context.</returns>
        protected async Task<CalculatorContext> CalculateChildPriceAsync(Product childProduct, CalculatorContext context)
        {
            if (context.Product == childProduct)
            {
                // TODO: (core) Decent error message for price calculation deadlocking.
                throw new InvalidOperationException("Deadlock");
            }
            
            var childCalculatorContext = new CalculatorContext(context, childProduct.Price)
            {
                Product = childProduct,
                Quantity = int.MaxValue
            };

            // INFO: we know that options have been cloned.
            childCalculatorContext.Options.BatchContext = context.Options.ChildProductsBatchContext;
            childCalculatorContext.Options.IgnoreGroupedProducts = true;
            childCalculatorContext.Options.IgnoreBundles = true;

            // Get calculators for child product context
            var calculators = _calculatorFactory.GetCalculators(childCalculatorContext);

            // Run calculators
            await _calculatorFactory.RunCalculators(calculators, childCalculatorContext);

            return childCalculatorContext;
        }
    }
}
