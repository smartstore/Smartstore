using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Product attribute materializer interface.
    /// </summary>
    public partial interface IProductAttributeMaterializer
    {
        /// <summary>
        /// Gets a list of product variant attributes.
        /// </summary>
        /// <param name="selection">Attributes selection.</param>
        /// <returns>List of product variant attributes.</returns>
        Task<IList<ProductVariantAttribute>> MaterializeProductVariantAttributesAsync(ProductVariantAttributeSelection selection);

        /// <summary>
        /// Gets a list of product variant attribute values.
        /// </summary>
        /// <param name="selection">Attributes selection.</param>
        /// <returns>List of product variant attribute values.</returns>
        Task<IList<ProductVariantAttributeValue>> MaterializeProductVariantAttributeValuesAsync(ProductVariantAttributeSelection selection);

        /// <summary>
        /// Gets a list of product variant attribute values.
        /// </summary>
        /// <param name="selection">Attributes selection.</param>
        /// <param name="attributes">Product variant attributes.</param>
        /// <returns>List of product variant attribute values.</returns>
        IList<ProductVariantAttributeValue> MaterializeProductVariantAttributeValues(ProductVariantAttributeSelection selection, IEnumerable<ProductVariantAttribute> attributes);

        /// <summary>
        /// Finds an attribute combination by attribute selection.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Found attribute combination or <c>null</c> if none was found.</returns>
        Task<ProductVariantAttributeCombination> FindAttributeCombinationAsync(int productId, ProductVariantAttributeSelection selection);

        /// <summary>
        /// Finds an attribute combination by attribute selection and applies its data to the product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Found attribute combination or <c>null</c> if none was found.</returns>
        Task<ProductVariantAttributeCombination> MergeWithCombinationAsync(Product product, ProductVariantAttributeSelection selection);

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
