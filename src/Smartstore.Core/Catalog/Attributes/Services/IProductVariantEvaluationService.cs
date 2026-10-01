#nullable enable

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Provides services for evaluating current product variant selections and hypothetical candidates.
/// Variant candidates are required to calculate prices for attribute swatches based on the selected attribute values.
/// </summary>
public partial interface IProductVariantEvaluationService
{
    /// <summary>
    /// Prepares the current product variant selection for a product.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The evaluation of the current product variant selection.</returns>
    Task<ProductVariantEvaluationResult> EvaluateCurrentAsync(ProductVariantEvaluationContext context);

    /// <summary>
    /// Creates hypothetical selections by replacing the selected value of an attribute.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="evaluation">The evaluation of the current product variant selection.</param>
    /// <param name="candidateValues">The attribute values to create candidates for.</param>
    /// <returns>The candidates keyed by product variant attribute value identifier.</returns>
    Task<IReadOnlyDictionary<int, ProductVariantCandidate>> EvaluateCandidatesAsync(
        ProductVariantEvaluationContext context,
        ProductVariantEvaluationResult evaluation,
        IEnumerable<ProductVariantAttributeValue> candidateValues);
}
