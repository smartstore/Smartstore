using Smartstore.Core.OutputCache;
using Smartstore.Utilities;

namespace Smartstore.Core.Configuration
{
    public static class IOutputCacheInvalidationObserverSettingExtensions
    {
        public static void ObserveSetting(this IOutputCacheInvalidationObserver observer, string settingKey)
        {
            observer.ObserveSetting(settingKey, null);
        }

        /// <summary>
        /// Registers a concrete setting class to be observed by the framework. If any setting property
        /// of <typeparamref name="TSetting"/> changes, the framework will purge the cache.
        /// </summary>
        /// <typeparam name="TSetting">The type of the concrete setting class to observe</typeparam>
        /// <remarks>
        /// A property observer precedes a class observer.
        /// </remarks>
        public static void ObserveSettings<TSetting>(this IOutputCacheInvalidationObserver observer)
            where TSetting : ISettings
        {
            ObserveSettings<TSetting>(observer, null);
        }

        /// <summary>
        /// Registers a concrete setting class to be observed by the framework. If any setting property
        /// of <typeparamref name="TSetting"/> changes, the framework will call the <paramref name="invalidationAction"/> handler.
        /// </summary>
        /// <typeparam name="TSetting">The type of the concrete setting class to observe</typeparam>
        /// <param name="invalidationAction">
        /// The invalidation action handler. If <c>null</c> is passed, the framework
        /// uses the default invalidator, which is <see cref="IOutputCacheProvider.RemoveAllAsync()"/>.
        /// </param>
        /// <remarks>
        /// A property observer precedes a class observer.
        /// </remarks>
        public static void ObserveSettings<TSetting>(
            this IOutputCacheInvalidationObserver observer,
            Func<IOutputCacheProvider, Task> invalidationAction) where TSetting : ISettings
        {
            var key = typeof(TSetting).Name + ".*";
            observer.ObserveSetting(key, invalidationAction);
        }

        /// <summary>
        /// Registers a setting property to be observed by the framework. If the value for the passed
        /// property changes, the framework will purge the cache.
        /// </summary>
        /// <typeparam name="TSetting">The type of the concrete setting class which contains the property</typeparam>
        /// <param name="propertyAccessor">The property lambda</param>
        public static void ObserveSettingProperty<TSetting>(
            this IOutputCacheInvalidationObserver observer,
            Expression<Func<TSetting, object>> propertyAccessor) where TSetting : ISettings
        {
            ObserveSettingProperty<TSetting>(observer, propertyAccessor, null);
        }

        /// <summary>
        /// Registers a setting property to be observed by the framework. If the value for the passed
        /// property changes, the framework will call the <paramref name="invalidationAction"/> handler.
        /// </summary>
        /// <typeparam name="TSetting">The type of the concrete setting class which contains the property</typeparam>
        /// <param name="propertyAccessor">The property lambda</param>
        /// <param name="invalidationAction">
        /// The invalidation action handler. If <c>null</c> is passed, the framework
        /// uses the default invalidator, which is <see cref="IOutputCacheProvider.RemoveAllAsync()"/>.
        /// </param>
        public static void ObserveSettingProperty<TSetting>(
            this IOutputCacheInvalidationObserver observer,
            Expression<Func<TSetting, object>> propertyAccessor,
            Func<IOutputCacheProvider, Task> invalidationAction) where TSetting : ISettings
        {
            Guard.NotNull(propertyAccessor, nameof(propertyAccessor));

            var key = TypeHelper.NameOf<TSetting>(propertyAccessor, true);
            observer.ObserveSetting(key, invalidationAction);
        }
    }
}
