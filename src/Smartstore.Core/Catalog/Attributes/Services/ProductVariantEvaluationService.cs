using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Default implementation of <see cref="IProductVariantEvaluationService"/>.
/// </summary>
public partial class ProductVariantEvaluationService : IProductVariantEvaluationService
{
    private readonly SmartDbContext _db;
    private readonly IProductAttributeMaterializer _productAttributeMaterializer;
    private readonly IRuleProviderFactory _ruleProviderFactory;

    /// <summary>
    /// Creates a new product variant evaluation service.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="productAttributeMaterializer">The product attribute materializer.</param>
    /// <param name="ruleProviderFactory">The rule provider factory.</param>
    public ProductVariantEvaluationService(
        SmartDbContext db,
        IProductAttributeMaterializer productAttributeMaterializer,
        IRuleProviderFactory ruleProviderFactory)
    {
        _db = db;
        _productAttributeMaterializer = productAttributeMaterializer;
        _ruleProviderFactory = ruleProviderFactory;
    }

    /// <inheritdoc/>
    public virtual async Task<ProductVariantEvaluationResult> EvaluateCurrentAsync(ProductVariantEvaluationContext context)
    {
        Guard.NotNull(context);

        var product = context.Product;
        var query = context.Query;
        var bundleItemId = context.BundleItem?.Id ?? 0;
        var attributes = await context.BatchContext.Attributes.GetOrLoadAsync(product.Id);

        if (query.VariantCombinationId == 0)
        {
            AddPreselectedAttributeValues(query, attributes, product.Id, bundleItemId, context.BundleItem);
        }

        var isSelectionSpecified = query.VariantCombinationId != 0 || query.Variants.Count > 0;
        var selection = new ProductVariantAttributeSelection(null);
        var warnings = new List<string>();

        if (query.VariantCombinationId != 0)
        {
            var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(query.VariantCombinationId, false);
            selection = new ProductVariantAttributeSelection(combination?.RawAttributes);
        }
        else if (isSelectionSpecified)
        {
            (selection, warnings) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(
                query,
                attributes,
                product.Id,
                bundleItemId,
                context.GetFilesFromRequest);
        }

        var unfilteredSelection = CloneSelection(selection);
        var inactiveAttributeIds = isSelectionSpecified
            ? await GetInactiveAttributeIdsAsync(context.Product, selection, GetRuleProvider(context))
            : [];

        if (inactiveAttributeIds.Count > 0)
        {
            selection.RemoveAttributes(inactiveAttributeIds);
        }

        return new()
        {
            Selection = selection,
            UnfilteredSelection = unfilteredSelection,
            SelectedValues = selection.MaterializeProductVariantAttributeValues(attributes).ToArray(),
            InactiveAttributeIds = inactiveAttributeIds,
            Combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection),
            Warnings = warnings.ToArray(),
            IsSelectionSpecified = isSelectionSpecified
        };
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyDictionary<int, ProductVariantCandidate>> EvaluateCandidatesAsync(
        ProductVariantEvaluationContext context,
        ProductVariantEvaluationResult evaluation,
        IEnumerable<ProductVariantAttributeValue> candidateValues)
    {
        Guard.NotNull(context);
        Guard.NotNull(evaluation);
        Guard.NotNull(candidateValues);

        var values = candidateValues.ToArray();
        if (values.Length == 0)
        {
            return new Dictionary<int, ProductVariantCandidate>();
        }

        var attributes = (await context.BatchContext.Attributes.GetOrLoadAsync(context.Product.Id)).ToArray();
        var combinations = await context.BatchContext.AttributeCombinations.GetOrLoadAsync(context.Product.Id);
        var listAttributeIds = attributes
            .Where(x => x.IsListTypeAttribute())
            .Select(x => x.Id)
            .ToHashSet();
        var combinationsByHashCode = combinations.ToLookup(x => x.GetAttributesHashCode());
        var requiredAttributes = attributes.Where(x => x.IsRequired).ToArray();
        var ruleProvider = GetRuleProvider(context);
        var result = new Dictionary<int, ProductVariantCandidate>();

        foreach (var value in values)
        {
            var candidateSelection = CloneSelection(evaluation.UnfilteredSelection);
            candidateSelection.RemoveAttribute(value.ProductVariantAttributeId);
            candidateSelection.AddAttributeValue(value.ProductVariantAttributeId, value.Id);

            var inactiveAttributeIds = await GetInactiveAttributeIdsAsync(context.Product, candidateSelection, ruleProvider);
            if (inactiveAttributeIds.Count > 0)
            {
                candidateSelection.RemoveAttributes(inactiveAttributeIds);
            }

            var combinationSelection = CreateListSelection(candidateSelection, listAttributeIds);
            var combination = combinationsByHashCode[combinationSelection.GetHashCode()]
                .FirstOrDefault(x => x.AttributeSelection.Equals(combinationSelection));
            var requiredAttributeIds = requiredAttributes
                .Where(x => !inactiveAttributeIds.Contains(x.Id))
                .Select(x => x.Id);

            result[value.Id] = new()
            {
                AttributeValue = value,
                Selection = candidateSelection,
                InactiveAttributeIds = inactiveAttributeIds,
                Combination = combination,
                SelectionHashCode = candidateSelection.GetHashCode(),
                HasRequiredSelections = requiredAttributeIds.All(x => HasSelection(candidateSelection, x))
            };
        }

        return result;
    }

    /// <summary>
    /// Gets the product attribute rule provider for an evaluation context.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The product attribute rule provider.</returns>
    protected virtual IAttributeRuleProvider GetRuleProvider(ProductVariantEvaluationContext context)
    {
        return _ruleProviderFactory.GetProvider<IAttributeRuleProvider>(
            RuleScope.ProductAttribute,
            new AttributeRuleProviderContext(context.Product.Id) { BatchContext = context.BatchContext });
    }

    /// <summary>
    /// Gets the identifiers of attributes that are inactive for a selection.
    /// </summary>
    /// <param name="product">The product whose attributes are evaluated.</param>
    /// <param name="selection">The product variant selection.</param>
    /// <param name="ruleProvider">The product attribute rule provider.</param>
    /// <returns>The identifiers of inactive product variant attributes.</returns>
    protected virtual async Task<IReadOnlyCollection<int>> GetInactiveAttributeIdsAsync(
        Product product,
        ProductVariantAttributeSelection selection,
        IAttributeRuleProvider ruleProvider)
    {
        var inactiveAttributes = await ruleProvider.GetInactiveAttributesAsync(product, selection);
        return inactiveAttributes.Select(x => x.Id).ToArray();
    }

    /// <summary>
    /// Adds values preselected by the merchant or bundle item filter to a variant query.
    /// </summary>
    /// <param name="query">The product variant query.</param>
    /// <param name="attributes">The product variant attributes.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="bundleItemId">The bundle item identifier.</param>
    /// <param name="bundleItem">The bundle item, if any.</param>
    protected virtual void AddPreselectedAttributeValues(
        ProductVariantQuery query,
        IEnumerable<ProductVariantAttribute> attributes,
        int productId,
        int bundleItemId,
        ProductBundleItem bundleItem)
    {
        foreach (var attribute in attributes.Where(x => x.IsListTypeAttribute()))
        {
            ProductVariantAttributeValue bundleDefaultValue = null;
            var availableValues = new List<ProductVariantAttributeValue>();

            foreach (var value in attribute.ProductVariantAttributeValues)
            {
                ProductBundleItemAttributeFilter attributeFilter = null;
                if (bundleItem?.IsFilteredOut(value, out attributeFilter) ?? false)
                {
                    continue;
                }

                availableValues.Add(value);

                if (bundleDefaultValue == null && attributeFilter?.IsPreSelected == true)
                {
                    bundleDefaultValue = value;
                }
            }

            IEnumerable<ProductVariantAttributeValue> selectedValues = bundleDefaultValue != null
                ? [bundleDefaultValue]
                : availableValues.Where(x => x.IsPreSelected);

            foreach (var value in selectedValues)
            {
                query.AddVariant(new()
                {
                    Value = value.Id.ToString(),
                    ProductId = productId,
                    BundleItemId = bundleItemId,
                    AttributeId = attribute.ProductAttributeId,
                    VariantAttributeId = attribute.Id,
                    Alias = attribute.ProductAttribute.Alias,
                    ValueAlias = value.Alias
                });
            }
        }
    }

    private static ProductVariantAttributeSelection CloneSelection(ProductVariantAttributeSelection selection)
        => new(selection?.AsJson());

    private static ProductVariantAttributeSelection CreateListSelection(ProductVariantAttributeSelection selection, HashSet<int> listAttributeIds)
    {
        var result = new ProductVariantAttributeSelection(null);

        foreach (var pair in selection.AttributesMap.Where(x => listAttributeIds.Contains(x.Key)))
        {
            result.AddAttribute(pair.Key, pair.Value);
        }

        return result;
    }

    private static bool HasSelection(ProductVariantAttributeSelection selection, int attributeId)
        => selection.GetAttributeValues(attributeId)?.Any(x => x?.ToString().HasValue() ?? false) ?? false;
}
