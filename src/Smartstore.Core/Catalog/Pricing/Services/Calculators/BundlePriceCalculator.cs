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

                context.FinalPrice = decimal.Zero;
                context.RegularPrice = decimal.Zero;

                foreach (var bundleItem in context.BundleItems)
                {
                    // Get the final unit price of bundle item part product.
                    // No need to pass bundleItem.Item.Quantity. The pipline always calculates a unit price.
                    var childCalculation = await CalculateChildPriceAsync(bundleItem.Product, context, c =>
                    {
                        c.Quantity = 1;
                        c.AssociatedProducts = null;
                        c.BundleItems = null;
                        c.BundleItem = bundleItem;
                    });

                    // Add price of part to root final price (unit price * contained quantity in this bundle).
                    context.FinalPrice += decimal.Multiply(childCalculation.FinalPrice, bundleItem.Quantity);
                    context.RegularPrice += decimal.Multiply(childCalculation.RegularPrice, bundleItem.Quantity);

                    // No copying of discounts to <see cref="CalculatorContext.AppliedDiscounts"/>. For BundlePerItemPricing they are only
                    // virtual stub discounts (<see cref="DiscountPriceCalculator"/> and <see cref="ProductBundleItem.Discount"/>).
                    context.DiscountAmount += decimal.Multiply(childCalculation.DiscountAmount, bundleItem.Quantity);

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
                    .Where(x => x != null)
                    .ToList();
            }

            if (options.ChildProductsBatchContext == null && context.BundleItems.Any(x => x.Product != null))
            {
                // Create a batch context with all bundle item products.
                var bundleItemProducts = context.BundleItems
                    .Where(x => x.Product != null)
                    .Select(x => x.Product);

                options.ChildProductsBatchContext = _productService.CreateProductBatchContext(bundleItemProducts, options.Store, options.Customer, false);
            }

            if (options.ChildProductsBatchContext != null)
            {
                options.BatchContext = options.ChildProductsBatchContext;
            }
        }
    }
}
