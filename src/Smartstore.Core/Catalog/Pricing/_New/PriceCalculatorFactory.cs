using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

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

            var cacheKey = "PriceCalculators:" + GenerateHashCode(context).ToString();

            return _requestCache.Get(cacheKey, () => 
            {
                return _lazyCalculators
                    .Where(x => MatchCalculator(context, x.Metadata))
                    .Select(x => x.Value)
                    .ToArray();
            });
        }

        protected virtual int GenerateHashCode(PriceCalculationContext context)
        {
            // TODO: (more) Add more parts to PriceCalculationContext hash
            return HashCode.Combine(context.Product.GetType());
        }

        protected virtual bool MatchCalculator(PriceCalculationContext context, PriceCalculatorMetadata metadata)
        {
            if (context.Product is Product)
            {
                return metadata.ValidTargets.HasFlag(CalculatorTargets.Product);
            }
            else if (context.Product is ShoppingCartItem)
            {
                return metadata.ValidTargets.HasFlag(CalculatorTargets.CartItem);
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
