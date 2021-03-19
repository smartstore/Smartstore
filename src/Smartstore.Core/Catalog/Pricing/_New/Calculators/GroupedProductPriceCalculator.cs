using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.CartItem, CalculatorOrdering.Early)]
    public class GroupedProductPriceCalculator : IPriceCalculator
    {
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IPriceCalculatorFactory _calculatorFactory;

        public GroupedProductPriceCalculator(ICatalogSearchService catalogSearchService, IPriceCalculatorFactory calculatorFactory)
        {
            _catalogSearchService = catalogSearchService;
            _calculatorFactory = calculatorFactory;
        }

        public ICommonServices Services { get; set; }

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            if (context.Product is Product product && product.ProductType == ProductType.GroupedProduct && !context.Options.IgnoreAssociatedProducts)
            {
                await EnsureAssociatedProductsAreLoaded(product, context);

                // TODO: Return if group has no associated products (?)

                Product lowestPriceProduct = null;

                foreach (var associatedProduct in context.AssociatedProducts)
                {
                    // Get the final price of associated product
                    var price = await GetFinalPrice(associatedProduct, context);

                    if (associatedProduct.LowestAttributeCombinationPrice < price)
                    {
                        // TODO: (core) Should we check IgnoreAttributes here and skip this?
                        price = associatedProduct.LowestAttributeCombinationPrice.Value;
                    }

                    if (!context.LowestPrice.HasValue || price < context.LowestPrice.Value)
                    {
                        // Set the lowest price
                        context.LowestPrice = price;
                        lowestPriceProduct = associatedProduct;
                    }
                }

                if (lowestPriceProduct == null)
                {
                    lowestPriceProduct = context.AssociatedProducts.FirstOrDefault();
                }

                if (context.LowestPrice.HasValue)
                {
                    // Favour lowest price
                    context.FinalPrice = context.LowestPrice.Value;
                }

                context.HasPriceRange = true;

                // Continue pipeline with actual child product
                context.Product = lowestPriceProduct;
            }

            await next(context);
        }

        private async Task EnsureAssociatedProductsAreLoaded(Product product, CalculatorContext context)
        {
            var options = context.Options;

            if (context.AssociatedProducts == null)
            {
                // Associated products have not been preloaded unfortunately. Get 'em here for this particular product.
                var searchQuery = new CatalogSearchQuery()
                    .PublishedOnly(true)
                    .HasStoreId(options.Store.Id)
                    .HasParentGroupedProduct(product.Id);

                var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                context.AssociatedProducts = (await searchResult.GetHitsAsync()).OrderBy(x => x.DisplayOrder).ToList();
            }

            if (options.AssociatedProductsBatchContext == null && context.AssociatedProducts.Any())
            {
                // No batch context given for the listing batch, so create one for associated products of this particular product.
                options.AssociatedProductsBatchContext = new ProductBatchContext(context.AssociatedProducts, Services, options.Store, options.Customer, false);
            }

            // Continue pipeline with AssociatedProductsBatchContext
            options.BatchContext = options.AssociatedProductsBatchContext;
        }

        /// <summary>
        /// Gets the final price for a child product by running a nested calculation pipeline.
        /// </summary>
        private async Task<decimal> GetFinalPrice(Product associatedProduct, CalculatorContext context)
        {
            var childCalculatorContext = new CalculatorContext(context, associatedProduct.Price)
            {
                Product = associatedProduct,
                Quantity = int.MaxValue
            };

            // INFO: we know that options have been cloned.
            childCalculatorContext.Options.BatchContext = context.Options.AssociatedProductsBatchContext;
            childCalculatorContext.Options.IgnoreAssociatedProducts = true;

            // Get calculators for associated product context
            var calculators = _calculatorFactory.GetCalculators(childCalculatorContext);

            // Run calculators
            await _calculatorFactory.RunCalculators(calculators, childCalculatorContext);

            return childCalculatorContext.FinalPrice;
        }
    }
}
