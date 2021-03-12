using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Smartstore.Data.Caching.Internal
{
    internal static class DbCachingEventId
    {
        /// <summary>
        /// The lower-bound for event IDs used by any Entity Framework or provider code.
        /// </summary>
        public const int CachingBaseId = 50000;

        // Warning: These values must not change between releases.
        // Only add new values to the end of sections, never in the middle.
        // Try to use <Noun><Verb> naming and be consistent with existing names.
        private enum Id
        {
            CacheHit = CachingBaseId,
            QueryResultCached,
            MaxRowsExceeded
        }

        private static readonly string _queryPrefix = DbLoggerCategory.Query.Name + ".";
        private static EventId MakeQueryId(Id id) => new EventId((int)id, _queryPrefix + id);

        /// <summary>
        /// A query result is returned from cache.
        /// </summary>
        public static readonly EventId CacheHit = MakeQueryId(Id.CacheHit);

        /// <summary>
        /// A query result is stored in cache.
        /// </summary>
        public static readonly EventId QueryResultCached = MakeQueryId(Id.QueryResultCached);

        /// <summary>
        /// Cannot store because max rows limit was exceeded.
        /// </summary>
        public static readonly EventId MaxRowsExceeded = MakeQueryId(Id.MaxRowsExceeded);
    }
}