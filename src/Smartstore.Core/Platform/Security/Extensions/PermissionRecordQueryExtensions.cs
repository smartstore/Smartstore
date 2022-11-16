namespace Smartstore.Core.Security
{
    public static partial class PermissionRecordQueryExtensions
    {
        /// <summary>
        /// Applies a filter for system names and sorts by <see cref="BaseEntity.Id"/>.
        /// </summary>
        /// <param name="query">Permission record query.</param>
        /// <param name="systemName">Permission system name.</param>
        /// <returns>Permission record query.</returns>
        public static IOrderedQueryable<PermissionRecord> ApplySystemNameFilter(this IQueryable<PermissionRecord> query, string systemName)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => x.SystemName == systemName);

            return query.OrderBy(x => x.Id);
        }
    }
}
