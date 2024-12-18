using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductReviewQueryExtensions
    {
        /// <summary>
        /// Filters away items in a query belonging to stores to which a given authenticated customer is not authorized to access.
        /// </summary>
        /// <param name="query">Query of type <see cref="ProductReview"/> from which to filter.</param>
        /// <param name="customerAuthorizedStores">The stores the authenticated customer is authorized to access.</param>
        /// <param name="storeMappings">The mappings of all items of type T belonging to a limited number of stores.</param>
        /// <returns><see cref="IQueryable"/> of <see cref="BaseEntity"/>.</returns>
        public static IQueryable<ProductReview> ApplyReviewStoreFilter(
            this IQueryable<ProductReview> query,
            int[] customerAuthorizedStores,
            StoreMappingCollection storeMappings)
        {
            if (customerAuthorizedStores.IsNullOrEmpty()) return query;
            Guard.NotNull(query, nameof(query));

            var groupedStoreMappings = storeMappings.GroupBy(
            sm => sm.EntityId,
            sm => sm.StoreId,
            (key, g) => new { EntityId = key, StoreIdsList = g.ToList() });

            foreach (var groupedMapping in groupedStoreMappings)
            {
                if (!customerAuthorizedStores.Any(casId => groupedMapping.StoreIdsList.Any(storeId => storeId == casId)))
                {
                    query = query.Where(x => x.ProductId != groupedMapping.EntityId);
                }
            }

            return query;
        }
    }
}
