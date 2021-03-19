using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public delegate Task CalculatorDelegate(CalculatorContext context);

    public interface IPriceCalculator
    {
        Task CalculateAsync(CalculatorContext context, CalculatorDelegate next);
    }

    public abstract class PriceCalculator : IPriceCalculator
    {
        public virtual async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            Calculate(context);
            await next(context);
        }

        protected virtual void Calculate(CalculatorContext context)
        {
        }
    }
}
