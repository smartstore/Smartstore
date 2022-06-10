namespace Smartstore.Core.Identity
{
    public static partial class CustomerRoleMappingQueryExtensions
    {
        /// <summary>
        /// Applies standard filters and sorts by <see cref="CustomerRoleMapping.IsSystemMapping"/>.
        /// </summary>
        /// <param name="query">Customer role mapping query.</param>
        /// <param name="customerRoleIds">Applies a filter by <see cref="CustomerRoleMapping.CustomerRoleId"/>.</param>
        /// <param name="isSystemMapping">Applies a filter by <see cref="CustomerRoleMapping.IsSystemMapping"/>.</param>
        /// <returns>Customer role mapping query.</returns>
        public static IOrderedQueryable<CustomerRoleMapping> ApplyStandardFilter(this IQueryable<CustomerRoleMapping> query, int[] customerRoleIds, bool? isSystemMapping = null)
        {
            Guard.NotNull(query, nameof(query));

            if (customerRoleIds?.Any() ?? false)
            {
                query = query.Where(x => customerRoleIds.Contains(x.CustomerRoleId));
            }

            if (isSystemMapping.HasValue)
            {
                query = query.Where(x => x.IsSystemMapping == isSystemMapping.Value);
            }

            return query.OrderBy(x => x.IsSystemMapping);
        }
    }
}
