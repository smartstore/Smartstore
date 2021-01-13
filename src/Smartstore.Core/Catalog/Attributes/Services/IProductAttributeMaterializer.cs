using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Product attribute materializer interface.
    /// </summary>
    public partial interface IProductAttributeMaterializer
    {
        /// <summary>
        /// Gets a list of product variant attribute values.
        /// </summary>
        /// <param name="selection">Attributes selection.</param>
        /// <param name="attributes">Product variant attributes.</param>
        /// <returns>Materialized product variant values.</returns>
        IList<ProductVariantAttributeValue> MaterializeProductVariantAttributeValues(ProductVariantAttributeSelection selection, IEnumerable<ProductVariantAttribute> attributes);

        /// <summary>
        /// Gets a value indicating whether two attribute selections are equal.
        /// </summary>
        /// <param name="selection1">First attribute selection.</param>
        /// <param name="selection2">Second attribute selection.</param>
        /// <returns>A value indicating whether two attribute selections are equal.</returns>
        bool AreProductAttributesEqual(AttributeSelection selection1, AttributeSelection selection2);

        /// <summary>
        /// Finds an attribute combination by attribute selection.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Found attribute combination or <c>null</c> if none was found.</returns>
        Task<ProductVariantAttributeCombination> FindAttributeCombinationAsync(int productId, ProductVariantAttributeSelection selection);

        /// <summary>
        /// Returns informations about the availability of an attribute combination.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="attributes">All product attributes of the specified product. <c>null</c> to test availability of <paramref name="selectedValues"/>.</param>
        /// <param name="selectedValues">The attribute values of the currently selected attribute combination.</param>
        /// <param name="currentValue">The current attribute value. <c>null</c> to test availability of <paramref name="selectedValues"/>.</param>
        /// <returns>Informations about the attribute combination's availability. <c>null</c> if the combination is available.</returns>
        Task<CombinationAvailabilityInfo> IsCombinationAvailableAsync(
            Product product,
            IEnumerable<ProductVariantAttribute> attributes,
            IEnumerable<ProductVariantAttributeValue> selectedValues,
            ProductVariantAttributeValue currentValue);
    }

    [Serializable]
    public class CombinationAvailabilityInfo
    {
        public bool IsActive { get; set; }
        public bool IsOutOfStock { get; set; }
    }
}
