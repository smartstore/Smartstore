#nullable enable

using Smartstore.Engine.Modularity;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Responsible for resolving <see cref="IAIProvider"/> instances.
    /// </summary>
    public partial interface IAIProviderFactory
    {
        /// <summary>
        /// Gets a list of all registered <see cref="IAIProvider"/> implementations.
        /// </summary>
        IReadOnlyList<Provider<IAIProvider>> GetAllProviders();

        /// <summary>
        /// Gets a list of <see cref="IAIProvider"/> implementations that support the given <paramref name="feature"/>.
        /// </summary>
        IReadOnlyList<Provider<IAIProvider>> GetProviders(AIProviderFeatures feature);

        /// <summary>
        /// Gets the first <see cref="IAIProvider"/> that supports <paramref name="feature"/>.
        /// </summary>
        Provider<IAIProvider>? GetFirstProvider(AIProviderFeatures feature);

        /// <summary>
        /// Gets <see cref="IAIProvider"/> by its system name.
        /// </summary>
        Provider<IAIProvider>? GetProviderBySystemName(string systemName);
    }
}
