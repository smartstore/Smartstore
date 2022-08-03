using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculatorFactory : IPriceCalculatorFactory
    {
        private readonly IRequestCache _requestCache;
        private readonly IEnumerable<Lazy<IPriceCalculator, PriceCalculatorMetadata>> _lazyCalculators;

        public PriceCalculatorFactory(IRequestCache requestCache, IEnumerable<Lazy<IPriceCalculator, PriceCalculatorMetadata>> calculators)
        {
            _requestCache = requestCache;
            _lazyCalculators = calculators.OrderBy(x => x.Metadata.Order);
        }

        public IPriceCalculator[] GetCalculators(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            if (context.Calculators != null && context.Calculators.Length > 0)
            {
                // Don't resolve calculators if context has a specific calculators list.
                return context.Calculators;
            }

            var cacheKey = "PriceCalculators:" + GenerateHashCode(context).ToString();

            var calculators = _requestCache.Get(cacheKey, () =>
            {
                return _lazyCalculators
                    .Where(x => MatchCalculator(context, x.Metadata))
                    .Select(x => x.Value)
                    .ToArray();
            });

            return calculators;
        }

        protected virtual int GenerateHashCode(PriceCalculationContext context)
        {
            // TODO: (more) Add more parts to PriceCalculationContext hash
            return context.Product.ProductTypeId;
        }

        protected virtual bool MatchCalculator(PriceCalculationContext context, PriceCalculatorMetadata metadata)
        {
            var productType = context.Product.ProductType;

            if (productType == ProductType.SimpleProduct)
            {
                return metadata.ValidTargets.HasFlag(CalculatorTargets.Product);
            }
            else if (productType == ProductType.BundledProduct)
            {
                return metadata.ValidTargets.HasFlag(CalculatorTargets.Bundle);
            }
            else if (productType == ProductType.GroupedProduct)
            {
                return metadata.ValidTargets.HasFlag(CalculatorTargets.GroupedProduct);
            }

            return false;
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
