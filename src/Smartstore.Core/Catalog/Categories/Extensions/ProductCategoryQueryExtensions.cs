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
        public static IOrderedQueryable<ProductCategory> ApplyCategoryFilter(
            this IQueryable<ProductCategory> query, 
            int categoryId, 
            bool? isSystemMapping = null)
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

        /// <summary>
        /// Applies a filter that reads all descendant nodes of the node with the given <paramref name="treePath"/>.
        /// </summary>
        /// <param name="treePath">The parent's tree path to get descendants from.</param>
        /// <param name="includeSelf"><c>true</c> = add the parent node to the result list, <c>false</c> = ignore the parent node.</param>
        public static IQueryable<ProductCategory> ApplyDescendantsFilter(
            this IQueryable<ProductCategory> query,
            string treePath,
            bool includeSelf = true)
        {
            Guard.NotNull(query);
            Guard.NotEmpty(treePath);

            if (treePath.Length < 3 || (treePath[0] != '/' && treePath[^1] != '/'))
            {
                throw new ArgumentException("Invalid treePath format.", nameof(treePath));
            }

            query = query.Where(x => x.Category.TreePath.StartsWith(treePath));

            if (!includeSelf)
            {
                query = query.Where(x => x.Category.TreePath.Length > treePath.Length);
            }

            return query;
        }
    }
}
