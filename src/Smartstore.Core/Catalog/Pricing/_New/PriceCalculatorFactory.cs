using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing.Calculators;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculatorFactory : IPriceCalculatorFactory
    {
        public IPriceCalculator[] GetCalculators(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            return GetCalculatorsCore(context).ToArray();
        }

        protected virtual List<IPriceCalculator> GetCalculatorsCore(PriceCalculationContext context)
        {
            return new List<IPriceCalculator> 
            {
                new OfferPriceCalculator()
            };
        }

        public async Task RunCalculators(IPriceCalculator[] calculators, CalculatorContext context)
        {
            Guard.NotNull(calculators, nameof(calculators));
            Guard.NotNull(context, nameof(context));

            var numCalculators = calculators.Length;
            if (numCalculators == 0)
            {
                return;
            }

            // Start the pipeline with first calculator middleware
            int i = 0;
            await Next(context);

            async Task Next(CalculatorContext ctx)
            {
                if (i >= numCalculators)
                {
                    return;
                }

                i++;
                await calculators[i - 1].CalculateAsync(ctx, Next);
            }
        }
    }
}
