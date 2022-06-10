using Smartstore.Data.Hooks;

namespace Smartstore.Core.OutputCache
{
    public class ObserveEntityContext
    {
        public IOutputCacheProvider OutputCacheProvider { get; set; }
        public IDisplayControl DisplayControl { get; set; }
        public BaseEntity Entity { get; set; }
        public IHookedEntity EntityEntry { get; set; }
        public bool Handled { get; set; }
        public ScopedServiceContainer ServiceContainer { get; set; }
    }

    /// <summary>
    /// Allows registration of output cache invalidation handlers
    /// </summary>
    public partial interface IOutputCacheInvalidationObserver
    {
        /// <summary>
        /// Registers an entity observer. The passed observer is responsible for invalidating the output cache
        /// by calling one of the invalidation methods in the <see cref="IOutputCacheProvider"/> instance.
        /// The observer must then set the <see cref="ObserveEntityContext.Handled"/> property to <c>true</c>
        /// to signal the framework that it should skip executing subsequent observers. 
        /// </summary>
        /// <param name="observer">The observer action</param>
        /// <remarks>
        /// The implementation of this interface is singleton scoped.
        /// Don't use objects with shorter lifetime in your handler as this will lead to memory leaks.
        /// If your handler needs to call service methods, resolve required services
        /// with <see cref="ObserveEntityContext.ServiceContainer"/>.
        /// </remarks>
        void ObserveEntity(Func<ObserveEntityContext, Task> observer);

        IEnumerable<Func<ObserveEntityContext, Task>> GetEntityObservers();

        /// <summary>
        /// Registers a setting key to be observed by the framework. If the value for the passed
        /// setting key changes, the framework calls the <paramref name="invalidationAction"/> handler.
        /// The key can either be fully qualified - e.g. "CatalogSettings.ShowProductSku" -,
        /// or prefixed - e.g. "CatalogSettings.*". The latter calls the invalidator when ANY CatalogSetting changes.
        /// </summary>
        /// <param name="invalidationAction">
        /// The invalidation action handler. If <c>null</c> is passed, the framework
        /// uses the default invalidator, which is <see cref="IOutputCacheProvider.RemoveAllAsync()"/>.
        /// </param>
        void ObserveSetting(string settingKey, Func<IOutputCacheProvider, Task> invalidationAction);

        Func<IOutputCacheProvider, Task> GetInvalidationActionForSetting(string settingKey);
    }

    public class NullOutputCacheInvalidationObserver : IOutputCacheInvalidationObserver
    {
        private static readonly IOutputCacheInvalidationObserver _instance = new NullOutputCacheInvalidationObserver();

        public static IOutputCacheInvalidationObserver Instance => _instance;

        public void ObserveEntity(Func<ObserveEntityContext, Task> observer)
        {
        }

        public IEnumerable<Func<ObserveEntityContext, Task>> GetEntityObservers()
            => Enumerable.Empty<Func<ObserveEntityContext, Task>>();

        public void ObserveSetting(string settingKey, Func<IOutputCacheProvider, Task> invalidationAction)
        {
        }

        public Func<IOutputCacheProvider, Task> GetInvalidationActionForSetting(string settingKey)
            => null;
    }
}
