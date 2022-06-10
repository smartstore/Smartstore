using Smartstore.Caching;

namespace Smartstore
{
    public static class ICacheManagerExtensions
    {
        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns the value to be added to the cache</param>
        /// <param name="independent">When <c>true</c>, no attemp will be made to invalidate depending/parent cache entries.</param>
        /// <param name="allowRecursion">When <c>false</c>, an exception will be thrown when the acquirer tries to acces the same cache item.</param>
        /// <returns>Cached item value</returns>
        public static T Get<T>(this ICacheManager cache, string key, Func<T> acquirer, bool independent = false, bool allowRecursion = false)
        {
            return cache.Get<T>(key, o => acquirer(), independent, allowRecursion);
        }

        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns the value to be added to the cache</param>
        /// <param name="independent">When <c>true</c>, no attemp will be made to invalidate depending/parent cache entries.</param>
        /// <param name="allowRecursion">When <c>false</c>, an exception will be thrown when the acquirer tries to acces the same cache item.</param>
        /// <returns>Cached item value</returns>
        public static Task<T> GetAsync<T>(this ICacheManager cache, string key, Func<CacheEntryOptions, T> acquirer, bool independent = false, bool allowRecursion = false)
        {
            return cache.GetAsync<T>(key, o => Task.FromResult(acquirer(o)), independent, allowRecursion);
        }

        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns the value to be added to the cache</param>
        /// <param name="independent">When <c>true</c>, no attemp will be made to invalidate depending/parent cache entries.</param>
        /// <param name="allowRecursion">When <c>false</c>, an exception will be thrown when the acquirer tries to acces the same cache item.</param>
        /// <returns>Cached item value</returns>
        public static Task<T> GetAsync<T>(this ICacheManager cache, string key, Func<T> acquirer, bool independent = false, bool allowRecursion = false)
        {
            return cache.GetAsync<T>(key, o => Task.FromResult(acquirer()), independent, allowRecursion);
        }

        /// <summary>
        /// Gets a cache item associated with the specified key or adds the item
        /// if it doesn't exist in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to get or add</typeparam>
        /// <param name="key">The cache item key</param>
        /// <param name="acquirer">Func which returns the value to be added to the cache</param>
        /// <param name="independent">When <c>true</c>, no attemp will be made to invalidate depending/parent cache entries.</param>
        /// <param name="allowRecursion">When <c>false</c>, an exception will be thrown when the acquirer tries to acces the same cache item.</param>
        /// <returns>Cached item value</returns>
        public static Task<T> GetAsync<T>(this ICacheManager cache, string key, Func<Task<T>> acquirer, bool independent = false, bool allowRecursion = false)
        {
            return cache.GetAsync<T>(key, o => acquirer(), independent, allowRecursion);
        }
    }
}
