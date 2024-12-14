using Smartstore.Engine.Modularity;

namespace Smartstore.Core.AI
{
    public partial class AIProviderFactory : IAIProviderFactory
    {
        private readonly IProviderManager _providerManager;
        private readonly Lazy<List<Provider<IAIProvider>>> _providers;

        public AIProviderFactory(IProviderManager providerManager)
        {
            _providerManager = providerManager;

            _providers = new Lazy<List<Provider<IAIProvider>>>(() =>
            {
                return [.. _providerManager.GetAllProviders<IAIProvider>()
                    .OrderBy(x => x.Metadata.DisplayOrder)
                    .ThenBy(x => x.Metadata.SystemName)];
            }, false);
        }

        public IReadOnlyList<Provider<IAIProvider>> GetAllProviders()
            => _providers.Value;

        public IReadOnlyList<Provider<IAIProvider>> GetProviders(AIProviderFeatures feature)
            => _providers.Value.Where(x => x.Value.Supports(feature) && x.Value.IsActive()).ToList();

        public Provider<IAIProvider> GetFirstProvider(AIProviderFeatures feature)
            => GetProviders(feature).FirstOrDefault();

        public Provider<IAIProvider> GetProviderBySystemName(string systemName)
            => _providers.Value.FirstOrDefault(x => x.Metadata.SystemName.EqualsNoCase(systemName));
    }
}
