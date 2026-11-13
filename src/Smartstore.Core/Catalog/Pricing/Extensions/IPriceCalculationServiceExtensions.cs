using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing;

public static partial class IPriceCalculationServiceExtensions
{
    /// <summary>
    /// Calculates unit prices for multiple calculation contexts. Prices are returned in the currency specified by <see cref="PriceCalculationOptions.TargetCurrency"/>.
    /// </summary>
    /// <param name="contexts">The contexts that contain the input products, calculation options and cargo data.</param>
    /// <returns>The calculated prices in the same order as the passed contexts.</returns>
    public static async Task<IReadOnlyList<CalculatedPrice>> CalculatePricesAsync(this IPriceCalculationService priceCalculationService, 
        IList<PriceCalculationContext> contexts,
        SmartDbContext dbContext)
    {
        Guard.NotNull(priceCalculationService);
        Guard.NotNull(dbContext);

        if (Guard.NotNull(contexts).Count == 0)
        {
            return [];
        }

        var contextsToPrepare = contexts.Where(x => x.LinkedProducts == null).ToArray();
        var linkedProductIds = new HashSet<int>();

        foreach (var context in contextsToPrepare)
        {
            foreach (var selectedAttributes in context.SelectedAttributes)
            {
                var batchContext = selectedAttributes.ProductId == context.Product.Id
                    ? context.Options.BatchContext
                    : context.Options.ChildProductsBatchContext;

                if (batchContext != null)
                {
                    var attributes = await batchContext.Attributes.GetOrLoadAsync(selectedAttributes.ProductId);
                    var values = selectedAttributes.Selection.MaterializeProductVariantAttributeValues(attributes);

                    linkedProductIds.AddRange(values
                        .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                        .Select(x => x.LinkedProductId));
                }
            }
        }

        if (linkedProductIds.Count > 0)
        {
            var linkedProducts = await dbContext.Products
                .AsNoTracking()
                .Where(x => linkedProductIds.Contains(x.Id))
                .SelectSummary()
                .ToDictionaryAsync(x => x.Id);

            contextsToPrepare.Each(x => x.LinkedProducts = linkedProducts);
        }

        var prices = await contexts
            .SelectAwait(async x => await priceCalculationService.CalculatePriceAsync(x))
            .ToListAsync();

        return prices;
    }

    /// <summary>
    /// Calculates the price adjustments of product attributes, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
    /// Typically used to display price adjustments of selected attributes on the cart page.
    /// The calculated adjustment is always a unit price.
    /// </summary>
    /// <param name="priceCalculationService">Price calculation service.</param>
    /// <param name="product">The product.</param>
    /// <param name="selection">Attribute selection. If <c>null</c> then the price adjustments of all attributes of <paramref name="product"/> are determined.</param>
    /// <param name="quantity">
    /// The product quantity. May have impact on the price, e.g. if tier prices are applied to price adjustments.
    /// Note that the calculated price is always the unit price.
    /// </param>
    /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
    /// <returns>Price adjustments of selected attributes. Key: <see cref="BaseEntity.Id"/>, value: attribute price adjustment.</returns>
    public static async Task<IDictionary<int, CalculatedPriceAdjustment>> CalculateAttributePriceAdjustmentsAsync(
        this IPriceCalculationService priceCalculationService,
        Product product,
        ProductVariantAttributeSelection selection = null,
        int quantity = 1,
        PriceCalculationOptions options = null)
    {
        Guard.NotNull(priceCalculationService);

        options ??= priceCalculationService.CreateDefaultOptions(false);

        var context = new PriceCalculationContext(product, quantity, options);
        context.Options.DeterminePriceAdjustments = true;
        context.Options.TaxFormat = null;

        context.AddSelectedAttributes(selection, product.Id);

        var price = await priceCalculationService.CalculatePriceAsync(context);
        return price.AttributePriceAdjustments.ToDictionarySafe(x => x.AttributeValue.Id);
    }

    /// <summary>
    /// Gets the base price info for a product.
    /// </summary>
    /// <param name="priceCalculationService">Price calculation service.</param>
    /// <param name="product">The product to get the base price info for.</param>
    /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
    /// <returns>Base price info.</returns>
    public static async Task<string> GetBasePriceInfoAsync(this IPriceCalculationService priceCalculationService, 
        Product product, 
        PriceCalculationOptions options = null)
    {
        Guard.NotNull(priceCalculationService);
        Guard.NotNull(product);

        if (!product.BasePriceHasValue || product.BasePriceAmount == 0)
        {
            return string.Empty;
        }

        options ??= priceCalculationService.CreateDefaultOptions(false);

        var context = new PriceCalculationContext(product, options);
        var price = await priceCalculationService.CalculatePriceAsync(context);

        return priceCalculationService.GetBasePriceInfo(
            product,
            price.FinalPrice,
            options.TargetCurrency,
            displayTaxSuffix: options.TaxFormat.IsEmpty() ? false : null);
    }
}