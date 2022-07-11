using Smartstore.Threading;

namespace Smartstore.Caching
{
    public enum CacheEntryRemovedReason
    {
        None,

        /// <summary>
        /// Manually
        /// </summary>
        Removed,

        /// <summary>
        /// Overwritten
        /// </summary>
        Replaced,

        /// <summary>
        /// Timed out
        /// </summary>
        Expired,

        /// <summary>
        /// Event
        /// </summary>
        TokenExpired,

        /// <summary>
        /// Overflow
        /// </summary>
        Capacity
    }

    public class CacheEntryExpiredEventArgs : EventArgs
    {
        public string Key { get; set; }
    }

    public class CacheEntryRemovedEventArgs : CacheEntryExpiredEventArgs
    {
        public CacheEntryRemovedReason Reason { get; set; }
        public CacheEntry Entry { get; set; }
    }

    /// <summary>
    /// Marker interface for an in-memory cache store.
    /// </summary>
    public interface IMemoryCacheStore : ICacheStore
    {
        event EventHandler<CacheEntryRemovedEventArgs> Removed;
    }

    /// <summary>
    /// Represents a distributed cache store, e.g. "Redis".
    /// </summary>
    public interface IDistributedCacheStore : ICacheStore
    {
        event EventHandler<CacheEntryExpiredEventArgs> Expired;

        /// <summary>
        /// Refreshes a value in the cache based on its key, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">key.</param>
        void Refresh(string key);

        /// <summary>
        /// Refreshes a value in the cache based on its key, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">key.</param>
        Task RefreshAsync(string key);

        /// <summary>
        /// Refreshes an entry, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">key.</param>
        /// <remarks>Used by hybrid cache to propagate access to a memory cache entry.</remarks>
        void Refresh(CacheEntry entry);

        /// <summary>
        /// Refreshes an entry, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">key.</param>
        /// <remarks>Used by hybrid cache to propagate access to a memory cache entry.</remarks>
        Task RefreshAsync(CacheEntry entry);
    }

    /// <summary>
    /// Represents an in-memory or a distributed cache store.
    /// </summary>
    public interface ICacheStore : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the cache is distributed (e.g. Redis)
        /// </summary>
        bool IsDistributed { get; }

        /// <summary>
        /// Gets a value indicating whether the entry associated with the specified key is cached already.
        /// </summary>
        /// <param name="key">key</param>
        bool Contains(string key);

        /// <summary>
        /// Gets a value indicating whether the entry associated with the specified key is cached already.
        /// </summary>
        /// <param name="key">key</param>
        Task<bool> ContainsAsync(string key);

        /// <summary>
        /// Gets a cache entry associated with the specified key
        /// </summary>
        /// <param name="key">The cache item key</param>
        /// <returns>Cached entry or <c>null</c> if item with specified key does not exist in the cache</returns>
        CacheEntry Get(string key);

        /// <summary>
        /// Gets a cache entry associated with the specified key
        /// </summary>
        /// <param name="key">The cache item key</param>
        /// <returns>Cached entry or <c>null</c> if item with specified key does not exist in the cache</returns>
        Task<CacheEntry> GetAsync(string key);

        /// <summary>
        /// Gets or creates a provider specific hashset implementation.
        /// If key does not exist, a new set is created and put to cache automatically
        /// </summary>
        /// <param name="key">The set cache item key</param>
        /// <param name="acquirer">Optional acquirer callback that is invoked when requested set does not exist yet.</param>
        /// <returns>The hashset</returns>
        ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null);

        /// <summary>
        /// Gets or creates a provider specific hashset implementation.
        /// If key does not exist, a new set is created and put to cache automatically
        /// </summary>
        /// <param name="key">The set cache item key</param>
        /// <param name="acquirer">Optional acquirer callback that is invoked when requested set does not exist yet.</param>
        /// <returns>The hashset</returns>
        Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null);

        /// <summary>
        /// Adds a cache entry with the specified key to the store.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="entry">Entry</param>
        void Put(string key, CacheEntry entry);

        /// <summary>
        /// Adds a cache entry with the specified key to the store.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="entry">Entry</param>
        Task PutAsync(string key, CacheEntry entry);

        /// <summary>
        /// Removes the entry with the specified key from the cache store.
        /// </summary>
        /// <param name="key">Key</param>
        void Remove(string key);

        /// <summary>
        /// Removes the entry with the specified key from the cache store.
        /// </summary>
        /// <param name="key">Key</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Removes all entries with keys matching the input pattern.
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">Glob pattern</param>
        /// <returns>Count of removed cache items</returns>
        long RemoveByPattern(string pattern);

        /// <summary>
        /// Removes all entries with keys matching the input pattern.
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">Glob pattern</param>
        /// <returns>Number of removed cache items</returns>
        Task<long> RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Scans for all keys matching the input pattern.
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">A key pattern. Can be <c>null</c>.</param>
        /// <returns>A list of matching key names</returns>
        IEnumerable<string> Keys(string pattern = "*");

        /// <summary>
        /// Scans for all keys matching the input pattern. 
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">A key pattern. Can be <c>null</c>.</param>
        /// <returns>A list of matching key names</returns>
        IAsyncEnumerable<string> KeysAsync(string pattern = "*");

        /// <summary>
        /// Gets a <see cref="IDistributedLock"/> instance for the given <paramref name="key"/>
        /// used to synchronize access to cache storage.
        /// </summary>
        IDistributedLock GetLock(string key);

        /// <summary>
        /// Clear all cache data
        /// </summary>
        void Clear();

        /// <summary>
        /// Clear all cache data
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Returns the remaining time to live of an entry that has a timeout.
        /// </summary>
        /// <returns>
        /// TTL, or <c>null</c> when key does not exist or does not have a timeout.
        /// </returns>
        TimeSpan? GetTimeToLive(string key);

        /// <summary>
        /// Returns the remaining time to live of an entry that has a timeout.
        /// </summary>
        /// <returns>
        /// TTL, or <c>null</c> when key does not exist or does not have a timeout.
        /// </returns>
        Task<TimeSpan?> GetTimeToLiveAsync(string key);

        /// <summary>
        /// Sets/updates a timeout on an entry. After the timeout has expired, the entry will automatically be deleted.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the timeout was set. <c>false</c> if key does not exist or the timeout could not be set.
        /// </returns>
        bool SetTimeToLive(string key, TimeSpan? duration);

        /// <summary>
        /// Sets/updates a timeout on an entry. After the timeout has expired, the entry will automatically be deleted.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the timeout was set. <c>false</c> if key does not exist or the timeout could not be set.
        /// </returns>
        Task<bool> SetTimeToLiveAsync(string key, TimeSpan? duration);
    }
}
