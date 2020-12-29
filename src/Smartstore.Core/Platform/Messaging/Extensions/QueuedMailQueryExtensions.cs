using System;
using System.Linq;
using Smartstore.Core.Messages;

namespace Smartstore
{
    public static class QueuedMailQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="QueuedEmail.From"/> and <see cref="QueuedEmail.To"/>.
        /// </summary>
        public static IQueryable<QueuedEmail> ApplyMailAddressFilter(this IQueryable<QueuedEmail> query, string from, string to)
        {
            Guard.NotNull(query, nameof(query));

            if (from.HasValue())
                query = query.Where(x => x.From.Contains(from.Trim()));

            if (to.HasValue())
                query = query.Where(x => x.To.Contains(to.Trim()));

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
        /// Applies order filter by <see cref="QueuedEmail.Priority"/> and <see cref="QueuedEmail.CreatedOnUtc"/>. 
        /// </summary>
        /// <param name="orderByLatest">Specifies sort order.</param>
        public static IQueryable<QueuedEmail> ApplyOrderFilter(this IQueryable<QueuedEmail> query, bool orderByLatest)
        {
            Guard.NotNull(query, nameof(query));

            query = query.OrderByDescending(x => x.Priority);

            query = orderByLatest ?
                ((IOrderedQueryable<QueuedEmail>)query).ThenByDescending(x => x.CreatedOnUtc) :
                ((IOrderedQueryable<QueuedEmail>)query).ThenBy(x => x.CreatedOnUtc);

            return query;
        }
    }
}
