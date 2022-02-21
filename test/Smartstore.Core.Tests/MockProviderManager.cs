using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Smartstore.Core.Configuration;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Tests
{
    public class MockProviderManager : IProviderManager
    {
        private IDictionary<ProviderMetadata, IProvider> _providers = new Dictionary<ProviderMetadata, IProvider>();

        public void RegisterProvider(string systemName, IProvider provider)
        {
            var metadata = new ProviderMetadata
            {
                SystemName = systemName
            };
            _providers[metadata] = provider;
        }

        public Provider<TProvider> GetProvider<TProvider>(string systemName, int storeId = 0) where TProvider : IProvider
        {
            return _providers
                .Where(x => x.Key.SystemName.EqualsNoCase(systemName))
                .Select(x => new Provider<TProvider>(new Lazy<TProvider, ProviderMetadata>(() => (TProvider)x.Value, x.Key)))
                .FirstOrDefault();
        }

        public Provider<IProvider> GetProvider(string systemName, int storeId = 0)
        {
            return _providers
                .Where(x => x.Key.SystemName.EqualsNoCase(systemName))
                .Select(x => new Provider<IProvider>(new Lazy<IProvider, ProviderMetadata>(() => x.Value, x.Key)))
                .FirstOrDefault();
        }

        public IEnumerable<Provider<TProvider>> GetAllProviders<TProvider>(int storeId = 0) where TProvider : IProvider
        {
            return _providers
                .Where(x => typeof(TProvider).IsAssignableFrom(x.Value.GetType()))
                .Select(x => new Provider<TProvider>(new Lazy<TProvider, ProviderMetadata>(() => (TProvider)x.Value, x.Key)));
        }

        public IEnumerable<Provider<IProvider>> GetAllProviders(int storeId = 0)
        {
            return _providers.Select(x => new Provider<IProvider>(new Lazy<IProvider, ProviderMetadata>(() => x.Value, x.Key)));
        }

        public T GetUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor)
        {
            throw new NotImplementedException();
        }

        public ApplySettingResult ApplyUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor)
        {
            throw new NotImplementedException();
        }

        public bool IsActiveForStore(IModuleDescriptor module, int storeId)
        {
            throw new NotImplementedException();
        }
    }
}
