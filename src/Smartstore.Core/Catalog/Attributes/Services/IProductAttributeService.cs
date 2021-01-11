using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Collections;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Product attribute service interface.
    /// </summary>
    public partial interface IProductAttributeService
    {
        /// <summary>
        /// Gets the export mappings for a given field prefix.
        /// </summary>
        /// <param name="fieldPrefix">The export field prefix, e.g. "gmc".</param>
        /// <returns>A multimap with export field names to <see cref="ProductAttribute.Id"/> mappings.</returns>
        Task<Multimap<string, int>> GetExportFieldMappingsAsync(string fieldPrefix);

        /// <summary>
        /// Gets product variant attribute mappings.
        /// </summary>
        /// <param name="productVariantAttributeIds">Enumerable of product variant attribute mapping identifiers.</param>
        /// <param name="attributes">Collection of already loaded product attribute mappings to reduce database round trips.</param>
        /// <returns>Product variant attribute mappings.</returns>
        Task<IList<ProductVariantAttribute>> GetProductVariantAttributesByIdsAsync(IEnumerable<int> productVariantAttributeIds, IEnumerable<ProductVariantAttribute> attributes = null);

        /// <summary>
        /// Copies attribute options to product variant attribute values commits them to the database. Existing values are ignored (identified by name field).
        /// </summary>
        /// <param name="productVariantAttribute">The product variant attribute mapping entity.</param>
        /// <param name="productAttributeOptionsSetId">Identifier of product attribute options set.</param>
        /// <param name="deleteExistingValues">A value indicating whether to delete all existing product variant attribute values.</param>
        /// <returns>Number of added product variant attribute values.</returns>
        Task<int> CopyAttributeOptionsAsync(ProductVariantAttribute productVariantAttribute, int productAttributeOptionsSetId, bool deleteExistingValues);
    }
}
