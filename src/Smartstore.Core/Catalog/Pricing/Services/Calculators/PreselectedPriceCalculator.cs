using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price that is initially displayed on the product detail page.
    /// Actually, it does not calculate anything, but applies the attribute combination price determined by the attributes pre-selected by the merchant.
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

            if (!options.DeterminePreselectedPrice || options.IgnoreAttributes)
            {
                // Proceed with pipeline and omit this calculator, it is made for pre-selected price calculation only.
                await next(context);
                return;
            }

            //if (!options.ApplyPreSelectedAttributes)
            //{
            //    throw new ArgumentException($"{nameof(PriceCalculationOptions.ApplyPreSelectedAttributes)} must be 'true' for PreselectedPriceCalculator to get pre-selected attribute values.");
            //}

            // TODOs:
            // - CatalogSettings.EnableDynamicPriceUpdate

            var selectedValues = (await context.GetPreSelectedAttributeValuesAsync())
                .Where(x => x.ProductVariantAttribute.IsListTypeAttribute())
                .ToList();

            if (selectedValues.Any())
            {
                var query = new ProductVariantQuery();
                var product = context.Product;
                var bundleItemId = context.BundleItem?.Item?.Id ?? 0;
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

                // Merge combination price.
                if ((selectedCombination?.IsActive ?? false) && selectedCombination.Price.HasValue)
                {
                    product.MergedDataValues = new Dictionary<string, object> { { "Price", selectedCombination.Price.Value } };

                    // Base price info actually not required during calculation but feels ok this way.
                    if (selectedCombination.BasePriceAmount.HasValue)
                        product.MergedDataValues.Add("BasePriceAmount", selectedCombination.BasePriceAmount.Value);

                    if (selectedCombination.BasePriceBaseAmount.HasValue)
                        product.MergedDataValues.Add("BasePriceBaseAmount", selectedCombination.BasePriceBaseAmount.Value);
                }
            }

            context.Quantity = 1;
            await next(context);
        }
    }
}
