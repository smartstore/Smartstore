using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price of a bundled product. If <see cref="Product.BundlePerItemPricing"/> is activated, 
    /// then the price for each bundle item is calculated and multiplied by <see cref="ProductBundleItem.Quantity"/>.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Bundle, CalculatorOrdering.Early)]
    public class BundlePriceCalculator : PriceCalculator
    {
        private readonly IProductService _productService;

        public BundlePriceCalculator(IPriceCalculatorFactory calculatorFactory, IProductService productService)
            : base(calculatorFactory)
        {
            _productService = productService;
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

            if (product.BundlePerItemPricing)
            {
                if (context.Options.DetermineLowestPrice)
                {
                    context.HasPriceRange = true;
                }

                await EnsureBundleItemsAreLoaded(product, context);

                foreach (var bundleItem in context.BundleItems)
                {
                    // Get the final unit price of bundle item part product.
                    // No need to pass bundleItem.Item.Quantity. The pipline always calculates a unit price.
                    var childCalculation = await CalculateChildPriceAsync(bundleItem.Item.Product, context, c => 
                    { 
                        c.Quantity = 1;
                        c.AssociatedProducts = null;
                        c.BundleItems = null;
                        c.BundleItem = bundleItem;
                        c.AdditionalCharge = decimal.Zero;
                        c.MinTierPrice = null;
                    });

                    // Add price of part to root final price (unit price + additional charge * contained quantity in this bundle).
                    context.FinalPrice += decimal.Multiply(childCalculation.FinalPrice + bundleItem.AdditionalCharge, bundleItem.Item.Quantity);

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
                context.BundleItems = (await options.BatchContext.ProductBundleItems.GetOrLoadAsync(product.Id))
                    .Select(x => new ProductBundleItemData(x))
                    .Where(x => x.Item != null)
                    .ToList();
            }

            if (options.ChildProductsBatchContext == null && context.BundleItems.Any())
            {
                // Create a batch context with all bundle item products.
                var bundleItemProducts = context.BundleItems.Select(x => x.Item.Product);

                options.ChildProductsBatchContext = _productService.CreateProductBatchContext(bundleItemProducts, options.Store, options.Customer, false);
            }

            options.BatchContext = options.ChildProductsBatchContext;
        }
    }
}
