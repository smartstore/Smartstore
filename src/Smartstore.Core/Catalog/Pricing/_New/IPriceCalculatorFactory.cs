using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial interface IPriceCalculatorFactory
    {
        IPriceCalculator[] GetCalculators(PriceCalculationContext context);
        Task RunCalculators(IPriceCalculator[] calculators, CalculatorContext context);
    }
}
