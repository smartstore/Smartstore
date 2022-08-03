using Autofac;
using Smartstore.Core.Configuration;

namespace Smartstore.Engine.Modularity
{
    public partial class ProviderManager : IProviderManager
    {
        private readonly IComponentContext _ctx;
        private readonly ISettingService _settingService;
        private readonly IModuleConstraint _moduleConstraint;

        public ProviderManager(
            IComponentContext ctx,
            ISettingService settingService,
            IModuleConstraint moduleConstraint)
        {
            _ctx = ctx;
            _settingService = settingService;
            _moduleConstraint = moduleConstraint;
        }

        public Provider<TProvider> GetProvider<TProvider>(string systemName, int storeId = 0) where TProvider : IProvider
        {
            if (systemName.IsEmpty())
                return null;

            var provider = _ctx.ResolveOptionalNamed<Lazy<TProvider, ProviderMetadata>>(systemName);

            if (provider != null)
            {
                if (storeId > 0)
                {
                    var d = provider.Metadata.ModuleDescriptor;
                    if (d != null && !_moduleConstraint.Matches(d, storeId))
                    {
                        return null;
                    }
                }

                SetUserData(provider.Metadata);
                return new Provider<TProvider>(provider);
            }

            return null;
        }

        public Provider<IProvider> GetProvider(string systemName, int storeId = 0)
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            var provider = _ctx.ResolveOptionalNamed<Lazy<IProvider, ProviderMetadata>>(systemName);

            if (provider != null)
            {
                if (storeId > 0)
                {
                    var d = provider.Metadata.ModuleDescriptor;
                    if (d != null && !_moduleConstraint.Matches(d, storeId))
                    {
                        return null;
                    }
                }

                SetUserData(provider.Metadata);
                return new Provider<IProvider>(provider);
            }

            return null;
        }

        public IEnumerable<Provider<TProvider>> GetAllProviders<TProvider>(int storeId = 0) where TProvider : IProvider
        {
            var providers = _ctx.Resolve<IEnumerable<Lazy<TProvider, ProviderMetadata>>>();

            if (storeId > 0)
            {
                providers = from p in providers
                            let d = p.Metadata.ModuleDescriptor
                            where d == null || _moduleConstraint.Matches(d, storeId)
                            select p;
            }

            return SortProviders(providers.Select(x => new Provider<TProvider>(x)));
        }

        public IEnumerable<Provider<IProvider>> GetAllProviders(int storeId = 0)
        {
            var providers = _ctx.Resolve<IEnumerable<Lazy<IProvider, ProviderMetadata>>>();

            if (storeId > 0)
            {
                providers = from p in providers
                            let d = p.Metadata.ModuleDescriptor
                            where d == null || _moduleConstraint.Matches(d, storeId)
                            select p;
            }

            return SortProviders(providers.Select(x => new Provider<IProvider>(x)));
        }

        protected virtual IEnumerable<Provider<TProvider>> SortProviders<TProvider>(IEnumerable<Provider<TProvider>> providers) where TProvider : IProvider
        {
            foreach (var m in providers.Select(x => x.Metadata))
            {
                SetUserData(m);
            }

            return providers.OrderBy(x => x.Metadata.DisplayOrder).ThenBy(x => x.Metadata.FriendlyName);
        }

        protected virtual void SetUserData(ProviderMetadata metadata)
        {
            if (!metadata.IsEditable)
                return;

            metadata.FriendlyName = GetUserSetting(metadata, x => x.FriendlyName);
            metadata.Description = GetUserSetting(metadata, x => x.Description);

            var displayOrder = GetUserSetting<int?>(metadata, x => x.DisplayOrder);
            if (displayOrder.HasValue)
            {
                metadata.DisplayOrder = displayOrder.Value;
            }
        }

        public bool IsActiveForStore(IModuleDescriptor module, int storeId)
        {
            return _moduleConstraint.Matches(module, storeId);
        }

        public T GetUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor)
        {
            Guard.NotNull(metadata, nameof(metadata));
            Guard.NotNull(propertyAccessor, nameof(propertyAccessor));

            var settingKey = metadata.SettingKeyPattern.FormatInvariant(metadata.SystemName, propertyAccessor.ExtractPropertyInfo().Name);
            return _settingService.GetSettingByKey<T>(settingKey);
        }

        public ApplySettingResult ApplyUserSetting<T>(ProviderMetadata metadata, Expression<Func<ProviderMetadata, T>> propertyAccessor)
        {
            Guard.NotNull(metadata, nameof(metadata));
            Guard.NotNull(propertyAccessor, nameof(propertyAccessor));

            var settingKey = metadata.SettingKeyPattern.FormatInvariant(metadata.SystemName, propertyAccessor.ExtractPropertyInfo().Name);
            var value = propertyAccessor.Compile().Invoke(metadata);

            return _settingService.ApplySettingAsync(settingKey, value).Await();
        }
    }
}
