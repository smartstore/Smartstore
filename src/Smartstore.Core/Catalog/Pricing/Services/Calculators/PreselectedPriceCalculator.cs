using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price that is initially displayed on the product detail page.
    /// Actually, it does not calculate anything, but applies the attribute combination price determined by the attributes preselected by the merchant.
    /// That's why this calculator must run very early.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Early + 1)]
    public class PreselectedPriceCalculator : IPriceCalculator
    {
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;

        public PreselectedPriceCalculator(IProductAttributeMaterializer productAttributeMaterializer)
        {
            _productAttributeMaterializer = productAttributeMaterializer;
        }

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var options = context.Options;

            if (!options.DeterminePreselectedPrice)
            {
                // Proceed with pipeline and omit this calculator, it is made for preselected price calculation only.
                await next(context);
                return;
            }

            var selectedValues = (await context.GetPreselectedAttributeValuesAsync())
                .Where(x => x.ProductVariantAttribute.IsListTypeAttribute())
                .ToList();

            if (selectedValues.Any())
            {
                // Create attribute selection of preselected values.
                var query = new ProductVariantQuery();
                var product = context.Product;
                var bundleItemId = context.BundleItem?.Id ?? 0;
                var attributes = await options.BatchContext.Attributes.GetOrLoadAsync(product.Id);
                var combinations = await options.BatchContext.AttributeCombinations.GetOrLoadAsync(product.Id);

                foreach (var value in selectedValues)
                {
                    var productAttribute = value.ProductVariantAttribute;

                    query.AddVariant(new ProductVariantQueryItem(value.Id.ToString())
                    {
                        ProductId = product.Id,
                        BundleItemId = bundleItemId,
                        AttributeId = productAttribute.ProductAttributeId,
                        VariantAttributeId = productAttribute.Id,
                        Alias = productAttribute.ProductAttribute.Alias,
                        ValueAlias = value.Alias
                    });
                }

                var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, attributes, product.Id, bundleItemId, false);
                var selectedCombination = combinations.FirstOrDefault(x => x.AttributeSelection.Equals(selection));

                // Apply attribute combination price.
                if ((selectedCombination?.IsActive ?? false) && selectedCombination.Price.HasValue)
                {
                    context.FinalPrice = selectedCombination.Price.Value;
                    context.RegularPrice = selectedCombination.Price.Value;
                    context.AppliedAttributeCombination = selectedCombination;

                    // That comes too late because regular price has already been passed to child CalculatorContext:
                    //product.MergedDataValues = new Dictionary<string, object> { { "Price", selectedCombination.Price.Value } };
                }
            }

            // The product page is always loaded with the default quantity of 1.
            context.Quantity = 1;
            await next(context);

            context.PreselectedPrice = context.FinalPrice;
        }
    }
}
