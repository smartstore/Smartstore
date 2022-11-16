using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Product attribute materializer interface.
    /// </summary>
    public partial interface IProductAttributeMaterializer
    {
        /// <summary>
        /// Prefetches and caches all passed attribute selections for the current request.
        /// </summary>
        /// <param name="selections">All attribute selections to prefetch.</param>
        /// <returns>Number of prefetched attribute selections.</returns>
        Task<int> PrefetchProductVariantAttributesAsync(IEnumerable<ProductVariantAttributeSelection> selections);

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
        /// Creates an attribute selection.
        /// </summary>
        /// <param name="query">Product variant query.</param>
        /// <param name="attributes">Product variant attributes for the product.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="bundleItemId">Bundle item identifier.</param>
        /// <param name="getFilesFromRequest">A value indicating whether to get the uploaded file from current request. 
        /// <c>false</c> to get the file GUID from the query object.</param>
        /// <returns>Created attribute selection and warnings, if any.</returns>
        Task<(ProductVariantAttributeSelection Selection, List<string> Warnings)> CreateAttributeSelectionAsync(
            ProductVariantQuery query,
            IEnumerable<ProductVariantAttribute> attributes,
            int productId,
            int bundleItemId,
            bool getFilesFromRequest = true);

        /// <summary>
        /// Clears cached product attribute and attribute values.
        /// </summary>
        void ClearCachedAttributes();

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
        /// <param name="combination">The attribute combination to be merged. Loaded by <see cref="FindAttributeCombinationAsync"/> if <c>null</c>.</param>
        /// <returns>Found attribute combination or <c>null</c> if none was found.</returns>
        Task<ProductVariantAttributeCombination> MergeWithCombinationAsync(
            Product product,
            ProductVariantAttributeSelection selection,
            ProductVariantAttributeCombination combination = null);

        /// <summary>
        /// For each cart item, finds an attribute combination by attribute selection and applies its data to the product.
        /// </summary>
        /// <param name="cartItems">Cart items.</param>
        /// <returns>Number of merged attribute combinations.</returns>
        Task<int> MergeWithCombinationAsync(IEnumerable<ShoppingCartItem> cartItems);

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
