namespace Smartstore.Caching
{
    /// <summary>
    /// Subscribes an object to memory cache events.
    /// </summary>
    public interface ICacheEvents
    {
        /// <summary>
        /// Called before the object is put into memory cache.
        /// </summary>
        void OnCache();

        /// <summary>
        /// Called after the object is removed from memory cache.
        /// </summary>
        /// <param name="reason">The removal reason.</param>
        void OnRemoved(IMemoryCacheStore sender, CacheEntryRemovedReason reason);
    }
}
