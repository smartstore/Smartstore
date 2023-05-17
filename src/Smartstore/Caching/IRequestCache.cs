#nullable enable

namespace Smartstore.Caching
{
    /// <summary>
    /// Request cache interface
    /// </summary>
    public interface IRequestCache
    {
        /// <summary>
        /// Gets the underlying store.
        /// </summary>
        IDictionary<object, object?> Items { get; }

        /// <summary>
        /// Gets a cache item associated with the specified key
        /// </summary>
        /// <typeparam name="T">The type of the item to get</typeparam>
        /// <param name="key">The cache item key</param>
        /// <returns>Cached item value or <c>null</c> if item with specified key does not exist in the cache</returns>
        T? Get<T>(object key);

        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns value to be added to the cache</param>
        /// <returns>Cached item value</returns>
        T Get<T>(object key, Func<T> acquirer);

        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns value to be added to the cache</param>
        /// <returns>Cached item value</returns>
        Task<T> GetAsync<T>(object key, Func<Task<T>> acquirer);

        /// <summary>
        /// Adds a cache item with the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        void Put(object key, object? value);

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        bool Contains(object key);

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">key</param>
        void Remove(object key);

        /// <summary>
        /// Clear all cache data
        /// </summary>
        void Clear();
    }

#nullable disable

    /// <summary>
    /// For testing purposes
    /// </summary>
    public sealed class NullRequestCache : IRequestCache
    {
        public static NullRequestCache Instance { get; } = new NullRequestCache();

        public IDictionary<object, object> Items
        {
            get => new Dictionary<object, object>();
        }

        public T Get<T>(object key)
            => default;

        public T Get<T>(object key, Func<T> acquirer)
            => acquirer == null ? default : acquirer();

        public Task<T> GetAsync<T>(object key, Func<Task<T>> acquirer)
            => acquirer == null ? default : acquirer();

        public void Put(object key, object value) { }

        public void Clear() { }

        public bool Contains(object key) => false;

        public void Remove(object key) { }
    }
}
