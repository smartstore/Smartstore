using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculatorFactory : IPriceCalculatorFactory
    {
        private readonly IEnumerable<Lazy<IPriceCalculator>> _lazyCalculators;

        public PriceCalculatorFactory(IEnumerable<Lazy<IPriceCalculator>> calculators)
        {
            _lazyCalculators = calculators;
        }

        public IPriceCalculator[] GetCalculators(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            return GetCalculatorsCore(context).ToArray();
        }

        protected virtual List<IPriceCalculator> GetCalculatorsCore(PriceCalculationContext context)
        {
            return _lazyCalculators
                .Select(x => x.Value)
                .ToList();
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
