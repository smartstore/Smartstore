using System.Runtime.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.JsonConverters;

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

    [JsonConverter(typeof(ObjectContainerJsonConverter))]
    public class CacheEntry : IObjectContainer, ICloneable<CacheEntry>
    {
        // Used for serialization compatibility
        [IgnoreDataMember]
        public static readonly string Version = "1";

        /// <summary>
        /// Gets the type of the cache entry value.
        /// <para>Might be useful for (de)serialization.</para>
        /// </summary>
        public Type ValueType { get; set; }

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
        //[JsonConverter(typeof(ObjectWrapperValueJsonConverter))]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the creation UTC date of the cache entry.
        /// </summary>
        public DateTimeOffset CachedOn { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the last accessed UTC date of the cache entry.
        /// </summary>
        /// <remarks>For future use.</remarks>
        public DateTimeOffset? LastAccessedOn { get; set; }

        /// <summary>
        /// Gets or sets the entries absolute expiration relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before
        //  it will be removed. This will not extend the entry lifetime beyond the absolute
        //  expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Whether time expiration policy should be applied to the entry
        /// in the underlying cache store. Defaults to <c>true</c>.
        /// </summary>
        [IgnoreDataMember]
        public bool ApplyTimeExpirationPolicy { get; set; } = true;

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

        [IgnoreDataMember]
        public bool CancelTokenSourceOnRemove { get; set; } = true;

        [IgnoreDataMember]
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Gets a value indicating whether the item is logically expired or not.
        /// Depending on the cache store provider, the item might still live in the cache although
        /// according to the expiration timeout, the item is already expired.
        /// </summary>
        [IgnoreDataMember]
        public bool HasExpired
        {
            get => GetTimeToLive() <= TimeSpan.Zero;
        }

        /// <summary>
        /// Returns the remaining time to live of an entry that has a timeout.
        /// </summary>
        /// <remarks>
        /// TTL, or <c>null</c> when entry does not have a timeout.
        /// </remarks>
        public TimeSpan? GetTimeToLive(DateTimeOffset? utcNow = null)
        {
            utcNow ??= DateTimeOffset.UtcNow;

            if (SlidingExpiration.HasValue)
            {
                var slidingTime = ((LastAccessedOn ?? CachedOn) + SlidingExpiration) - utcNow;

                if (AbsoluteExpiration.HasValue)
                {
                    var absTime = (CachedOn + AbsoluteExpiration) - utcNow;
                    // Don't go beyond absolute expiration.
                    return slidingTime < absTime ? slidingTime : absTime;
                }
                else
                {
                    return slidingTime;
                }
            }
            else if (AbsoluteExpiration.HasValue)
            {
                return (CachedOn + AbsoluteExpiration) - utcNow;
            }

            return null;
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
                AbsoluteExpiration = this.AbsoluteExpiration,
                SlidingExpiration = this.SlidingExpiration,
                ApplyTimeExpirationPolicy = this.ApplyTimeExpirationPolicy,
                CancelTokenSourceOnRemove = this.CancelTokenSourceOnRemove
            };
        }
    }
}