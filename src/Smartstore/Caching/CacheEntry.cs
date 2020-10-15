using System;
using System.Threading;
using Newtonsoft.Json;

namespace Smartstore.Caching
{
    public class CacheEntry : IObjectWrapper
    {
        /// <summary>
        /// Gets or sets the cache entry key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the cache entry value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets the type of the cache entry value.
        /// <para>Might be useful for (de)serialization.</para>
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the cache entry.
        /// </summary>
        public DateTimeOffset CachedOn { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the last accessed date of the cache entry.
        /// </summary>
        /// <remarks>For future use.</remarks>
        public DateTimeOffset LastAccessedOn { get; set; }

        /// <summary>
        /// Gets or sets the entries expiration timeout.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the keys of dependant (child) cache entries.
        /// If any of these entries are removed from cáche, this item will also be removed.
        /// </summary>
        public string[] Dependencies { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Gets a value indicating whether the item is logically expired or not.
        /// Depending on the cache store provider, the item might still live in the cache although
        /// according to the expiration timeout, the item is already expired.
        /// </summary>
        [JsonIgnore]
        public bool HasExpired
        {
            get
            {
                if (Duration == null)
                {
                    return false;
                }

                return CachedOn.Add(Duration.Value) < DateTimeOffset.UtcNow;
            }
        }

        /// <summary>
        ///  Returns the remaining time to live of an entry that has a timeout.
        /// </summary>
        /// <remarks>
        /// TTL, or <c>null</c> when entry does not have a timeout.
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? TimeToLive
        {
            get => Duration.HasValue ? CachedOn.Add(Duration.Value) - DateTimeOffset.UtcNow : null;
        }
    }
}