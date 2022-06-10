using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;

namespace Smartstore
{
    public static class QueuedEmailQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="QueuedEmail.From"/> and <see cref="QueuedEmail.To"/>.
        /// </summary>
        public static IQueryable<QueuedEmail> ApplyMailAddressFilter(this IQueryable<QueuedEmail> query, string from, string to)
        {
            Guard.NotNull(query, nameof(query));

            if (from.HasValue())
                query = query.ApplySearchFilterFor(x => x.From, from.Trim());

            if (to.HasValue())
                query = query.ApplySearchFilterFor(x => x.To, to.Trim());

            return query;
        }

        /// <summary>
        /// Applies filter by <see cref="QueuedEmail.CreatedOnUtc"/> and <see cref="QueuedEmail.SentOnUtc"/>. 
        /// </summary>
        public static IQueryable<QueuedEmail> ApplyTimeFilter(this IQueryable<QueuedEmail> query, DateTime? startTime, DateTime? endTime, bool unsentOnly)
        {
            Guard.NotNull(query, nameof(query));

            if (startTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc >= startTime);

            if (endTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc <= endTime);

            if (unsentOnly)
                query = query.Where(x => !x.SentOnUtc.HasValue);

            return query;
        }

        /// <summary>
        /// Applies sorting by <see cref="QueuedEmail.Priority"/> and <see cref="QueuedEmail.CreatedOnUtc"/>. 
        /// </summary>
        /// <param name="sortByLatest">If <c>true</c>, sorts <see cref="QueuedEmail.CreatedOnUtc"/> descending.</param>
        public static IOrderedQueryable<QueuedEmail> ApplySorting(this IQueryable<QueuedEmail> query, bool sortByLatest)
        {
            Guard.NotNull(query, nameof(query));

            var orderedQuery = query.OrderByDescending(x => x.Priority);

            orderedQuery = sortByLatest ?
                orderedQuery.ThenByDescending(x => x.CreatedOnUtc) :
                orderedQuery.ThenBy(x => x.CreatedOnUtc);

            return orderedQuery;
        }
    }
}
