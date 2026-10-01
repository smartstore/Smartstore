#nullable enable

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Contains the evaluation of a product variant selection and its related data.
/// </summary>
public partial class ProductVariantEvaluationResult
{
    /// <summary>
    /// Gets the effective selection after inactive attributes have been removed.
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
    /// Gets the attribute combination matching the effective selection.
    /// </summary>
    public ProductVariantAttributeCombination? Combination { get; init; }

    /// <summary>
    /// Gets warnings that occurred while materializing the selection.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether queried or preselected variant data was available.
    /// </summary>
    public bool IsSelectionSpecified { get; init; }

    /// <summary>
    /// Gets or sets hypothetical selections keyed by product variant attribute value identifier.
    /// </summary>
    public IReadOnlyDictionary<int, ProductVariantCandidate> Candidates { get; set; }
        = new Dictionary<int, ProductVariantCandidate>();
}
