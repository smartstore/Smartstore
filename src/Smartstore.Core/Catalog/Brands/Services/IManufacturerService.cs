namespace Smartstore.Core.Catalog.Brands
{
    /// <summary>
    /// Manufacturer service interface.
    /// </summary>
    public partial interface IManufacturerService
    {
        /// <summary>
        /// Gets product manufacturer mappings by product identifiers.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden manufacturers.</param>
        /// <returns>Product manufacturers.</returns>
        Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdsAsync(int[] productIds, bool includeHidden = false);
    }
}
