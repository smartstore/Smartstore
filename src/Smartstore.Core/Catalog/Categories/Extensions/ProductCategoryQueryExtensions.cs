namespace Smartstore.Core.Catalog.Categories
{
    public static partial class ProductCategoryQueryExtensions
    {
        /// <summary>
        /// Applies a category filter and sorts by <see cref="ProductCategory.DisplayOrder"/>, then by <see cref="BaseEntity.Id"/>
        /// Includes <see cref="ProductCategory.Category"/> and <see cref="ProductCategory.Product"/>.
        /// </summary>
        /// <param name="query">Product category query.</param>
        /// <param name="categoryId">Applies filter by <see cref="ProductCategory.CategoryId"/>.</param>
        /// <param name="isSystemMapping">Applies filter by <see cref="ProductCategory.IsSystemMapping"/></param>
        /// <returns>Product category query.</returns>
        public static IOrderedQueryable<ProductCategory> ApplyCategoryFilter(this IQueryable<ProductCategory> query, int categoryId, bool? isSystemMapping = null)
        {
            Guard.NotNull(query, nameof(query));

            query = query
                .Include(x => x.Category)
                .Include(x => x.Product)
                .Where(x => x.CategoryId == categoryId && x.Category != null && x.Product != null);

            if (isSystemMapping.HasValue)
            {
                query = query.Where(x => x.IsSystemMapping == isSystemMapping.Value);
            }

            return query
                .OrderBy(pc => pc.DisplayOrder)
                .ThenBy(pc => pc.Id);
        }
    }
}
