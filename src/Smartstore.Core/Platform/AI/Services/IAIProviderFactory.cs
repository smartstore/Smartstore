#nullable enable

using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Responsible for resolving <see cref="IAIProvider"/> instances.
    /// </summary>
    public partial interface IAIProviderFactory
    {
        /// <summary>
        /// Gets a list of all <see cref="IAIProvider"/>.
        /// </summary>
        IList<Provider<IAIProvider>> GetAllProviders();

        /// <summary>
        /// Gets a list of <see cref="IAIProvider"/> that supports <paramref name="feature"/>.
        /// </summary>
        IList<Provider<IAIProvider>> GetProviders(AIProviderFeatures feature);

        /// <summary>
        /// Gets the first <see cref="IAIProvider"/> that supports <paramref name="feature"/>.
        /// </summary>
        Provider<IAIProvider>? GetFirstProvider(AIProviderFeatures feature);

        /// <summary>
        /// Gets <see cref="IAIProvider"/> by its systemname.
        /// </summary>
        Provider<IAIProvider>? GetProviderBySystemName(string systemName);
    }
}
