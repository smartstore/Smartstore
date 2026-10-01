#nullable enable

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Represents a hypothetical product variant selection for an attribute value.
/// </summary>
public partial class ProductVariantCandidate
{
    /// <summary>
    /// Gets the attribute value represented by the candidate.
    /// </summary>
    public required ProductVariantAttributeValue AttributeValue { get; init; }

    /// <summary>
    /// Gets the effective candidate selection.
    /// </summary>
    public required ProductVariantAttributeSelection Selection { get; init; }

    /// <summary>
    /// Gets the identifiers of attributes that are inactive for the candidate selection.
    /// </summary>
    public IReadOnlyCollection<int> InactiveAttributeIds { get; init; } = [];

    /// <summary>
    /// Gets the attribute combination matching the candidate selection.
    /// </summary>
    public ProductVariantAttributeCombination? Combination { get; init; }

    /// <summary>
    /// Gets the hash code of the effective candidate selection.
    /// </summary>
    public int SelectionHashCode { get; init; }

    /// <summary>
    /// Gets a value indicating whether all required active attributes have been selected.
    /// </summary>
    public bool HasRequiredSelections { get; init; }
}
