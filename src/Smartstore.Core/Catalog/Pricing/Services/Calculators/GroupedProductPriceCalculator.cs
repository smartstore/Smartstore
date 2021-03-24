using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) Describe
    /// </summary>
    [CalculatorUsage(CalculatorTargets.GroupedProduct, CalculatorOrdering.Early)]
    public class GroupedProductPriceCalculator : PriceCalculator
    {
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IProductService _productService;

        public GroupedProductPriceCalculator(ICatalogSearchService catalogSearchService, IPriceCalculatorFactory calculatorFactory, IProductService productService)
            : base(calculatorFactory)
        {
            _catalogSearchService = catalogSearchService;
            _productService = productService;
        }

        public override async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;

            if (product.ProductType != ProductType.GroupedProduct)
            {
                // Proceed with pipeline and omit this calculator, it is made for grouped products only.
                await next(context);
                return;
            }

            await EnsureAssociatedProductsAreLoaded(product, context);

            if (context.AssociatedProducts.Count == 0)
            {
                // No children, get out.
                return;
            }

            var options = context.Options;

            CalculatorContext lowestPriceCalculation = null;

            if (options.DetermineLowestPrice && context.AssociatedProducts.Count > 1)
            {
                foreach (var associatedProduct in context.AssociatedProducts)
                {
                    // Get the final price of associated product
                    var childCalculation = await CalculateChildPriceAsync(associatedProduct, context, c => 
                    { 
                        c.Quantity = int.MaxValue;
                        c.AssociatedProducts = null;
                    });

                    if (lowestPriceCalculation == null || childCalculation.FinalPrice < lowestPriceCalculation.FinalPrice)
                    {
                        // Set the lowest price calculation
                        lowestPriceCalculation = childCalculation;
                    }
                }
            }
            else
            {
                // Get the final price of first associated product
                lowestPriceCalculation = await CalculateChildPriceAsync(context.AssociatedProducts.First(), context, c => 
                { 
                    c.Quantity = int.MaxValue;
                    c.AssociatedProducts = null;
                });
            }

            lowestPriceCalculation.HasPriceRange = context.AssociatedProducts.Count > 1;

            // Copy data from child context to this context
            lowestPriceCalculation.CopyTo(context);
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

            if (options.ChildProductsBatchContext == null && context.AssociatedProducts.Any())
            {
                // No batch context given for the listing batch, so create one for associated products of this particular product.
                options.ChildProductsBatchContext = _productService.CreateProductBatchContext(context.AssociatedProducts, options.Store, options.Customer, false);
            }

            // Continue pipeline with AssociatedProductsBatchContext
            options.BatchContext = options.ChildProductsBatchContext;
        }
    }
}
