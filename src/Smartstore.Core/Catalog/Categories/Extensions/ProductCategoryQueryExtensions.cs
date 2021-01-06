using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Categories
{
    public static partial class ProductCategoryQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Category.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product category query.</param>
        /// <param name="customerRolesIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Product category query.</returns>
        public static IOrderedQueryable<ProductCategory> ApplyStandardFilter(
            this IQueryable<ProductCategory> query,
            int[] customerRolesIds = null,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            var db = query.GetDbContext<SmartDbContext>();

            if (storeId > 0)
            {
                var categoriesQuery = db.Categories.ApplyStoreFilter(storeId);

                query =
                    from pc in query
                    join m in categoriesQuery on pc.CategoryId equals m.Id
                    select pc;
            }

            if (customerRolesIds != null)
            {
                var categoriesQuery = db.Categories.ApplyAclFilter(customerRolesIds);

                query =
                    from pc in query
                    join m in categoriesQuery on pc.CategoryId equals m.Id
                    select pc;
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}
