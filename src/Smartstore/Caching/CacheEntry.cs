using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Smartstore.Caching
{
    /// <summary>
    /// Specifies how items are prioritized for preservation during a memory pressure triggered cleanup.
    /// </summary>
    public enum CacheEntryPriority
    {
        Low,
        Normal,
        High,
        NeverRemove,
    }

    public class CacheEntry : IObjectWrapper, ICloneable<CacheEntry>
    {
        // Used for serialization compatibility
        [JsonIgnore]
        public static readonly string Version = "1";

        /// <summary>
        /// Gets or sets the cache entry key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a custom tag. May be evaluated in event handlers.
        /// </summary>
        public string Tag { get; set; }

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
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        /// memory pressure triggered cleanup. Only applies to memory cache.
        /// The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        public CacheEntryPriority Priority { get; set; } = CacheEntryPriority.Normal;

        /// <summary>
        /// Gets or sets the keys of dependant (child) cache entries.
        /// If any of these entries are removed from cáche, this item will also be removed.
        /// </summary>
        public string[] Dependencies { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public bool CancelTokenSourceOnRemove { get; set; } = true;

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

        object ICloneable.Clone() => Clone();
        public CacheEntry Clone()
        {
            // INFO: Never copy CancelTokenSource
            return new CacheEntry
            {
                Key = this.Key,
                Value = this.Value,
                ValueType = this.ValueType,
                Dependencies = this.Dependencies,
                LastAccessedOn = this.LastAccessedOn,
                CachedOn = DateTime.UtcNow,
                Duration = this.TimeToLive,
                CancelTokenSourceOnRemove = this.CancelTokenSourceOnRemove
            };
        }
    }
}