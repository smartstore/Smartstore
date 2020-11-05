using System;
using System.Collections.Generic;

namespace Smartstore.Data.Caching2
{
    /// <summary>
    /// DbCachingPolicy specifies the expiration time and dependencies of the cache entry.
    /// </summary>
    public class DbCachingPolicy
    {
        public DbCachingPolicy()
        {
        }

        public DbCachingPolicy(CacheableEntityAttribute attribute)
        {
            NoCaching = attribute.NeverCache;
            
            if (attribute?.Expiry > 0)
            {
                ExpirationTimeout = TimeSpan.FromMinutes(attribute.Expiry);
            }

            if (attribute?.MaxRows > 0)
            {
                MaxRows = attribute.MaxRows;
            }
        }

        internal bool NoCaching { get; set; }

        public int MaxRows { get; internal set; }

        public TimeSpan ExpirationTimeout { get; internal set; }

        public ISet<string> CacheItemDependencies { get; internal set; } = new SortedSet<string>();

        /// <summary>
        /// Set this option to the `real` related table names of the current query, if you are using a stored procedure,
        /// otherwise cache dependencies of normal queries will be calculated automatically.
        /// `cacheDependencies` determines which tables are used in this final query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public DbCachingPolicy WithDependencies(params string[] cacheDependencies)
        {
            CacheItemDependencies = new SortedSet<string>(cacheDependencies);
            return this;
        }

        /// <summary>
        /// Sets the expiration timeout. Default value is 1 day.
        /// </summary>
        public DbCachingPolicy ExpiresIn(TimeSpan timeout)
        {
            ExpirationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Specifies a max rows limit. Query results with more items than the given number will not be cached.
        /// </summary>
        public DbCachingPolicy WithMaxRowsLimit(int limit)
        {
            MaxRows = limit;
            return this;
        }

        public static string Configure(Action<DbCachingPolicy> options)
        {
            var cachePolicy = new DbCachingPolicy();
            options.Invoke(cachePolicy);
            return cachePolicy.ToString();
        }
    }
}