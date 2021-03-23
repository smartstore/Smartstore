using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.Bundle, CalculatorOrdering.Early)]
    public class BundlePriceCalculator : PriceCalculator
    {
        public BundlePriceCalculator(IPriceCalculatorFactory calculatorFactory)
            : base(calculatorFactory)
        {
        }

        public override async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;

            if (product.ProductType != ProductType.BundledProduct)
            {
                // Proceed with pipeline and omit this calculator, it is made for product bundles only.
                await next(context);
                return;
            }

            var options = context.Options;

            if (product.BundlePerItemPricing)
            {
                await EnsureBundleItemsAreLoaded(product, context);

                foreach (var bundleItem in context.BundleItems)
                {
                    // Get the final unit price of bundle item part product
                    // TODO: (mg) (core) Does it make more sense to pass contained quantity here (bundleItem.Item.Quantity) instead of multiplying later.
                    // TODO: (mg) (core) I think we need some sort of BundleItemsBatchContext here performance-wise (like we build and pass for grouped products)?
                    var childCalculation = await CalculateChildPriceAsync(bundleItem.Item.Product, context, c => 
                    { 
                        c.Quantity = 1;
                        c.AdditionalCharge = bundleItem.AdditionalCharge;
                        c.BundleItem = bundleItem; 
                    });

                    // Add price of part to root final price (unit price * contained quantity in this bundle)
                    context.FinalPrice += decimal.Multiply(childCalculation.FinalPrice, bundleItem.Item.Quantity);

                    // TODO: (mg) (core) Is it not better to continue the pipeline here (unlike in Smartstore classic)? Continuation could
                    // apply OfferPrice and/or further discounts to the automatically calculated final price here. TBD with MC please.
                }
            }
            else
            {
                // Continue pipeline
                await next(context);
            }
        }

        private async Task EnsureBundleItemsAreLoaded(Product product, CalculatorContext context)
        {
            var options = context.Options;

            if (context.BundleItems == null)
            {
                // Associated bundle items have not been preloaded unfortunately. Get 'em here for this particular product.
                if (!options.BatchContext.ProductBundleItems.FullyLoaded)
                {
                    await options.BatchContext.ProductBundleItems.LoadAllAsync();
                }

                context.BundleItems = (await options.BatchContext.ProductBundleItems.GetOrLoadAsync(product.Id))
                    .Select(x => new ProductBundleItemData(x))
                    .Where(x => x.Item != null)
                    .ToList();
            }
        }
    }
}
