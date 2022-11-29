using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;

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
        /// <returns>A multimap with export field names to <c>ProductAttribute.Id</c> mappings.</returns>
        Task<Multimap<string, int>> GetExportFieldMappingsAsync(string fieldPrefix);

        ///// <summary>
        ///// Gets product variant attribute mappings.
        ///// </summary>
        ///// <param name="productVariantAttributeIds">Enumerable of product variant attribute mapping identifiers.</param>
        ///// <param name="attributes">Collection of already loaded product attribute mappings to reduce database round trips.</param>
        ///// <returns>Product variant attribute mappings.</returns>
        //Task<IList<ProductVariantAttribute>> GetProductVariantAttributesByIdsAsync(IEnumerable<int> productVariantAttributeIds, IEnumerable<ProductVariantAttribute> attributes = null);

        /// <summary>
        /// Copies attribute options to product variant attribute values and commits them to the database. Existing values are ignored (identified by name field).
        /// </summary>
        /// <param name="productVariantAttribute">The product variant attribute mapping entity.</param>
        /// <param name="productAttributeOptionsSetId">Identifier of product attribute options set.</param>
        /// <param name="deleteExistingValues">A value indicating whether to delete all existing product variant attribute values.</param>
        /// <returns>Number of added product variant attribute values.</returns>
        Task<int> CopyAttributeOptionsAsync(ProductVariantAttribute productVariantAttribute, int productAttributeOptionsSetId, bool deleteExistingValues);

        /// <summary>
        /// Gets a distinct list of media file identifiers.
        /// Only files that are explicitly assigned to combinations are taken into account.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <returns>List of media file identifiers.</returns>
        Task<ICollection<int>> GetAttributeCombinationFileIdsAsync(Product product);

        /// <summary>
        /// Gets a distinct list of media file identifiers.
        /// Only files that are explicitly assigned to combinations are taken into account.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>List of media file identifiers.</returns>
        Task<ICollection<int>> GetAttributeCombinationFileIdsAsync(int productId);

        /// <summary>
        /// Creates all variant attributes combinations for a product.
        /// Already existing combinations will be deleted before.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>Number of added attribute combinations.</returns>
        Task<int> CreateAllAttributeCombinationsAsync(int productId);
    }
}
