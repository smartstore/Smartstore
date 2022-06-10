using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Categories
{
    public static partial class CategoryQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Category.ParentCategoryId"/>, then by <see cref="Category.DisplayOrder"/>, then by <see cref="Category.Name"/>.
        /// </summary>
        /// <param name="query">Category query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Category.Published"/>.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Category query.</returns>
        public static IOrderedQueryable<Category> ApplyStandardFilter(
            this IQueryable<Category> query,
            bool includeHidden = false,
            int[] customerRoleIds = null,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            if (customerRoleIds != null)
            {
                query = query.ApplyAclFilter(customerRoleIds);
            }

            return query
                .OrderBy(x => x.ParentCategoryId)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }
    }
}
