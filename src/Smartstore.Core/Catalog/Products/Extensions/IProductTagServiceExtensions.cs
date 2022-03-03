using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class IProductTagServiceExtensions
    {
        /// <summary>
        /// Gets the number of products associated with a product tag.
        /// </summary>
        /// <param name="productTagId">Product tag identifier.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. 0 to ignore store mappings.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden product tags. Hidden products are always ignored.</param>
        /// <returns>Number of products.</returns>
        public static async Task<int> CountProductsByTagIdAsync(this IProductTagService productTagService,
            int productTagId,
            Customer customer = null,
            int storeId = 0,
            bool includeHidden = false)
        {
            Guard.NotNull(productTagService, nameof(productTagService));

            if (productTagId == 0)
            {
                return 0;
            }

            var counts = await productTagService.GetProductCountsMapAsync(customer, storeId, includeHidden);
            return counts.Get(productTagId);
        }
    }
}
