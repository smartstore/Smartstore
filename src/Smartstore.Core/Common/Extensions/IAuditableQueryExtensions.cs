namespace Smartstore
{
    public static partial class IAuditableQueryExtensions
    {
        /// <summary>
        /// Applies a filter for <see cref="IAuditable.CreatedOnUtc"/>
        /// </summary>
        /// <param name="fromUtc">Start date in UTC.</param>
        /// <param name="toUtc">End date in UTC</param>
        public static IQueryable<T> ApplyAuditDateFilter<T>(this IQueryable<T> query, DateTime? fromUtc = null, DateTime? toUtc = null)
            where T : BaseEntity, IAuditable
        {
            Guard.NotNull(query, nameof(query));

            if (fromUtc.HasValue)
            {
                query = query.Where(x => fromUtc.Value <= x.CreatedOnUtc);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => toUtc.Value >= x.CreatedOnUtc);
            }

            return query;
        }
    }
}
