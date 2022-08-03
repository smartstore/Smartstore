using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Brands
{
    public static partial class ManufacturerQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Manufacturer.DisplayOrder"/>, then by <see cref="Manufacturer.Name"/>.
        /// </summary>
        /// <param name="query">Manufacturer query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Manufacturer.Published"/>.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Manufacturer query.</returns>
        public static IOrderedQueryable<Manufacturer> ApplyStandardFilter(
            this IQueryable<Manufacturer> query,
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
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }
    }
}
