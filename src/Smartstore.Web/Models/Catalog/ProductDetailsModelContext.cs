using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Models.Catalog;

public partial class ProductDetailsModelContext
{
    public ProductDetailsModelContext()
    {
    }

    /// <summary>
    /// Applies the property references of another <see cref="ProductDetailsModelContext"/> instance.
    /// Only to be used for child items like associated products or bundle items,
    /// otherwise use <see cref="CatalogHelper.CreateModelContext"/>.
    /// </summary>
    public ProductDetailsModelContext(ProductDetailsModelContext other)
    {
        Product = other.Product;
        BatchContext = other.BatchContext;
        VariantQuery = other.VariantQuery;
        Customer = other.Customer;
        Store = other.Store;
        Currency = other.Currency;
        DisplayPrices = other.DisplayPrices;
        HasInitiallySelectedVariants = false;

        AssociatedProducts = other.AssociatedProducts;
        GroupedProductConfiguration = other.GroupedProductConfiguration;
    }

    public Product Product { get; set; }
    public ProductBatchContext BatchContext { get; set; }
    public ProductVariantQuery VariantQuery { get; set; }
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public Currency Currency { get; set; }
    public bool DisplayPrices { get; set; }

    public bool IsAssociatedProduct { get; set; }
    public IList<Product> AssociatedProducts { get; set; }
    public GroupedProductConfiguration GroupedProductConfiguration { get; set; }

    public Product ParentProduct { get; set; }
    public ProductBundleItem ProductBundleItem { get; set; }

    /// <summary>
    /// Gets or sets the prepared product variant selection and its related data.
    /// </summary>
    public ProductVariantEvaluation VariantEvaluation { get; set; }

    /// <summary>
    /// Gets a value indicating whether processing was started with initially selected variants.
    /// </summary>
    public bool HasInitiallySelectedVariants { get; init; }
}

/// <summary>
/// Contains the prepared product variant selection and its related data.
/// </summary>
public partial class ProductVariantEvaluation
{
    /// <summary>
    /// Gets a value indicating whether queried or preselected variant data exists.
    /// </summary>
    public bool HasSelection { get; init; }

    /// <summary>
    /// Gets the selection after inactive attributes have been removed.
    /// </summary>
    public ProductVariantAttributeSelection Selection { get; init; } = new(null);

    /// <summary>
    /// Gets the selection before inactive attributes have been removed.
    /// </summary>
    public ProductVariantAttributeSelection UnfilteredSelection { get; init; } = new(null);

    /// <summary>
    /// Gets the selected list-type attribute values.
    /// </summary>
    public IReadOnlyCollection<ProductVariantAttributeValue> SelectedValues { get; init; } = [];

    /// <summary>
    /// Gets the identifiers of inactive product variant attributes.
    /// </summary>
    public IReadOnlyCollection<int> InactiveAttributeIds { get; init; } = [];

    /// <summary>
    /// Gets the attribute combination matching the selection.
    /// </summary>
    public ProductVariantAttributeCombination Combination { get; init; }

    /// <summary>
    /// Gets the candidates prepared for variant price calculation.
    /// </summary>
    public IReadOnlyCollection<ProductVariantCandidate> Candidates { get; init; } = [];

    /// <summary>
    /// Gets the calculated candidate prices by product variant attribute value identifier.
    /// </summary>
    public IReadOnlyDictionary<int, CalculatedPrice> Prices { get; init; } = new Dictionary<int, CalculatedPrice>();
}

/// <summary>
/// Contains a candidate product variant selection and its related data.
/// </summary>
public partial class ProductVariantCandidate
{
    /// <summary>
    /// Gets the attribute value applied to the candidate selection.
    /// </summary>
    public ProductVariantAttributeValue AttributeValue { get; init; }

    /// <summary>
    /// Gets the effective candidate selection after inactive attributes have been removed.
    /// </summary>
    public ProductVariantAttributeSelection Selection { get; init; }

    /// <summary>
    /// Gets the attribute combination matching the candidate selection.
    /// </summary>
    public ProductVariantAttributeCombination Combination { get; init; }
}
