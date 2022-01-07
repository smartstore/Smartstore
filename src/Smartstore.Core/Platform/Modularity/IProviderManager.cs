using Smartstore.Core.Configuration;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Manages provider implementations
    /// </summary>
    public interface IProviderManager
    {
        /// <summary>
        /// Gets a provider of type <typeparamref name="TProvider"/> by system name.
        /// </summary>
        Provider<TProvider> GetProvider<TProvider>(string systemName, int storeId = 0) where TProvider : IProvider;

        /// <summary>
        /// Gets a provider by system name.
        /// </summary>
        Provider<IProvider> GetProvider(string systemName, int storeId = 0);

        /// <summary>
        /// Enumerates all providers of type <typeparamref name="TProvider"/> lazily without instantiating them.
        /// </summary>
        IEnumerable<Provider<TProvider>> GetAllProviders<TProvider>(int storeId = 0) where TProvider : IProvider;

        /// <summary>
        /// Enumerates all providers lazily without instantiating them.
        /// </summary>
        IEnumerable<Provider<IProvider>> GetAllProviders(int storeId = 0);

        /// <summary>
        /// Gets a user setting for the given provider.
        /// </summary>
        T GetUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor);

        /// <summary>
        /// Applies a user setting for the given provider. The caller is responsible for database commit.
        /// </summary>
        ApplySettingResult ApplyUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor);

        /// <summary>
        /// Checks whether a given <paramref name="module"/> is activated for a particular <paramref name="storeId"/>.
        /// </summary>
        /// <param name="module">Module to check</param>
        /// <param name="storeId">Store ID to check.</param>
        bool IsActiveForStore(IModuleDescriptor module, int storeId);
    }
}