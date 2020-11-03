using System;
using System.Collections.Generic;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// EFCachePolicy specifies the expiration time and dependencies of the cache entry.
    /// </summary>
    public class EfCachePolicy
    {
        #region Consts

        /// <summary>
        /// It's `|`
        /// </summary>
        public const char ItemsSeparator = '|';

        /// <summary>
        /// It's `-->`
        /// </summary>
        public const string PartsSeparator = "-->";

        /// <summary>
        /// It's `_`
        /// </summary>
        public const string CacheDependenciesSeparator = "_";

        /// <summary>
        /// A special key for unknown cache dependencies
        /// </summary>
        public const string EfUnknownCacheDependency = nameof(EfUnknownCacheDependency);

        #endregion

        public EfCachePolicy()
        {
        }

        public EfCachePolicy(CacheableEntityAttribute attribute)
        {
            if (attribute?.Expiry > 0)
            {
                ExpirationTimeout = TimeSpan.FromMinutes(attribute.Expiry);
            }

            if (attribute?.MaxRows > 0)
            {
                MaxRows = attribute.MaxRows;
            }

            RequestCacheEnabled = attribute?.RequestCaching == true;
        }

        public int MaxRows { get; internal set; }

        public TimeSpan ExpirationTimeout { get; internal set; }

        public bool RequestCacheEnabled { get; internal set; }

        public ISet<string> CacheItemDependencies { get; internal set; } = new SortedSet<string>();

        /// <summary>
        /// Set this option to the `real` related table names of the current query, if you are using a stored procedure,
        /// otherwise cache dependencies of normal queries will be calculated automatically.
        /// `cacheDependencies` determines which tables are used in this final query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public EfCachePolicy WithDependencies(params string[] cacheDependencies)
        {
            CacheItemDependencies = new SortedSet<string>(cacheDependencies);
            return this;
        }

        /// <summary>
        /// Sets the expiration timeout. Default value is 1 day.
        /// </summary>
        public EfCachePolicy ExpiresIn(TimeSpan timeout)
        {
            ExpirationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Specifies whether the query result should be inserted to the request scoped cache also.
        /// Getting items from request cache does not come with the overhead of materializing the cached data reader.
        /// </summary>
        public EfCachePolicy WithRequestCache(bool enable)
        {
            RequestCacheEnabled = enable;
            return this;
        }

        /// <summary>
        /// Specifies a max rows limit. Query results with more items than the given number will not be cached.
        /// </summary>
        public EfCachePolicy WithMaxRowsLimit(int limit)
        {
            MaxRows = limit;
            return this;
        }

        public static string Configure(Action<EfCachePolicy> options)
        {
            var cachePolicy = new EfCachePolicy();
            options.Invoke(cachePolicy);
            return cachePolicy.ToString();
        }

        /// <summary>
        /// Represents the textual form of the current object
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(EfCachePolicy)} {PartsSeparator} {ExpirationTimeout}{ItemsSeparator}{MaxRows}{ItemsSeparator}{RequestCacheEnabled}{ItemsSeparator}{string.Join(CacheDependenciesSeparator, CacheItemDependencies)}".TrimEnd(ItemsSeparator);
        }
    }
}