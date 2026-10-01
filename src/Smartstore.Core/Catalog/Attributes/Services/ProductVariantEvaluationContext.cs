#nullable enable

using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Contains input data required for evaluating product variants.
/// </summary>
public partial class ProductVariantEvaluationContext
{
    /// <summary>
    /// Creates a new product variant evaluation context.
    /// </summary>
    /// <param name="product">The product whose variant selection is evaluated.</param>
    /// <param name="query">The queried product variant data.</param>
    /// <param name="batchContext">The product batch context.</param>
    public ProductVariantEvaluationContext(
        Product product,
        ProductVariantQuery query,
        ProductBatchContext batchContext)
    {
        Product = Guard.NotNull(product);
        Query = Guard.NotNull(query);
        BatchContext = Guard.NotNull(batchContext);
    }

    /// <summary>
    /// Gets the product whose variant selection is evaluated.
    /// </summary>
    public Product Product { get; }

    /// <summary>
    /// Gets the queried product variant data.
    /// </summary>
    public ProductVariantQuery Query { get; }

    /// <summary>
    /// Gets the product batch context.
    /// </summary>
    public ProductBatchContext BatchContext { get; }

    /// <summary>
    /// Gets the bundle item if the product is processed as part of a bundle.
    /// </summary>
    public ProductBundleItem? BundleItem { get; init; }

    /// <summary>
    /// Gets a value indicating whether uploaded files are read from the current request.
    /// </summary>
    public bool GetFilesFromRequest { get; init; } = true;
}
