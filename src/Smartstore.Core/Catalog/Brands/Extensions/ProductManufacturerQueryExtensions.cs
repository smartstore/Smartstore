using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Brands
{
    public static partial class ProductManufacturerQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Manufacturer.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product manufacturer query.</param>
        /// <param name="customerRolesIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Product manufacturer query.</returns>
        public static IOrderedQueryable<ProductManufacturer> ApplyStandardFilter(
            this IQueryable<ProductManufacturer> query,
            int[] customerRolesIds = null,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            var db = query.GetDbContext<SmartDbContext>();

            if (storeId > 0)
            {
                var manufacturersQuery = db.Manufacturers.ApplyStoreFilter(storeId);

                query =
                    from pm in query
                    join m in manufacturersQuery on pm.ManufacturerId equals m.Id
                    select pm;
            }

            if (customerRolesIds != null)
            {
                var manufacturersQuery = db.Manufacturers.ApplyAclFilter(customerRolesIds);

                query =
                    from pm in query
                    join m in manufacturersQuery on pm.ManufacturerId equals m.Id
                    select pm;
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}
