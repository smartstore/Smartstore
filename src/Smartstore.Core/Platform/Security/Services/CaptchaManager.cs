using System.Diagnostics.CodeAnalysis;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Security
{
    public class CaptchaManager : ICaptchaManager
    {
        private readonly CaptchaSettings _captchaSettings;
        private readonly IProviderManager _providerManager;

        public CaptchaManager(CaptchaSettings captchaSettings, IProviderManager providerManager)
        {
            _captchaSettings = captchaSettings;
            _providerManager = providerManager;
        }

        public Provider<ICaptchaProvider> GetCurrentProvider()
            => _providerManager.GetProvider<ICaptchaProvider>(_captchaSettings.ProviderSystemName);

        public Provider<ICaptchaProvider> GetProviderBySystemName(string systemName)
            => _providerManager.GetProvider<ICaptchaProvider>(systemName);

        public IEnumerable<Provider<ICaptchaProvider>> ListProviders()
            => _providerManager.GetAllProviders<ICaptchaProvider>().OrderBy(x => x.Metadata.DisplayOrder);

        public string[] GetActiveTargets()
            => _captchaSettings.ShowOn;

        public bool IsActiveTarget(string target)
            => _captchaSettings.IsActiveTarget(target);

        public bool IsConfigured([NotNullWhen(true)] out Provider<ICaptchaProvider> currentProvider)
        {
            currentProvider = null;

            if (_captchaSettings.Enabled && _captchaSettings.ShowOn.Length > 0)
            {
                currentProvider = GetCurrentProvider();
                return currentProvider?.Value?.IsConfigured == true;
            }

            return false;
        }
    }
}
