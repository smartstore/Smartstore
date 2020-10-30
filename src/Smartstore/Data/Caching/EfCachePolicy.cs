using System;
using System.Collections.Generic;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// EFCachePolicy determines the Expiration time of the cache.
    /// </summary>
    public class EfCachePolicy
    {
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

        /// <summary>
        /// The expiration timeout. Default value is 1 hour.
        /// </summary>
        public TimeSpan CacheTimeout { get; private set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Determines which entities are used in this LINQ query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public ISet<string> CacheItemsDependencies { get; private set; } = new SortedSet<string>();

        /// <summary>
        /// Determines the default Cacheable method
        /// </summary>
        public bool IsDefaultCacheableMethod { set; get; }

        /// <summary>
        /// Set this option to the `real` related table names of the current query, if you are using a stored procedure,
        /// otherwise cache dependencies of normal queries will be calculated automatically.
        /// `cacheDependencies` determines which tables are used in this final query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public EfCachePolicy CacheDependencies(params string[] cacheDependencies)
        {
            CacheItemsDependencies = new SortedSet<string>(cacheDependencies);
            return this;
        }

        /// <summary>
        /// The expiration timeout.
        /// Its default value is 1 hour.
        /// </summary>
        public EfCachePolicy Timeout(TimeSpan timeout)
        {
            CacheTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Determines the default Cacheable method
        /// </summary>
        public EfCachePolicy DefaultCacheableMethod(bool state)
        {
            IsDefaultCacheableMethod = state;
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
            return $"{nameof(EfCachePolicy)} {PartsSeparator} {CacheTimeout}{ItemsSeparator}{string.Join(CacheDependenciesSeparator, CacheItemsDependencies)}{ItemsSeparator}{IsDefaultCacheableMethod}".TrimEnd(ItemsSeparator);
        }
    }
}