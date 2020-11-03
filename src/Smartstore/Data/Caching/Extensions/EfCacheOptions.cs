using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public class EfCacheOptions
    {
        /// <summary>
        /// Should the debug level logging be disabled?
        /// </summary>
        public bool DisableLogging { set; get; }

        /// <summary>
        /// Gets or sets the default (fallback) expiration timeout for cacheable entities.
        /// Default is 1 day.
        /// </summary>
        public TimeSpan DefaultExpirationTimeout { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Gets or sets the default (fallback) max rows limit.
        /// Default is 1000 rows.
        /// </summary>
        public int DefaultMaxRows { get; set; } = 1000;
    }
}
