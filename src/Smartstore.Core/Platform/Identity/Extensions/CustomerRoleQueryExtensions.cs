namespace Smartstore.Core.Identity
{
    public static partial class CustomerRoleQueryExtensions
    {
        /// <summary>
        /// Applies standard filters and sorts by <see cref="CustomerRole.Name"/>.
        /// </summary>
        /// <param name="query">Customer role query.</param>
        /// <param name="includeHidden">Applies a filter by <see cref="CustomerRole.Active"/>.</param>
        /// <returns>Customer role query.</returns>
        public static IOrderedQueryable<CustomerRole> ApplyStandardFilter(this IQueryable<CustomerRole> query, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (includeHidden)
            {
                query = query.Where(x => x.Active);
            }

            return query.OrderBy(x => x.Name);
        }
    }
}
