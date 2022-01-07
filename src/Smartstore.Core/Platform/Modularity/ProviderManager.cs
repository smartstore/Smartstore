using Autofac;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;

namespace Smartstore.Engine.Modularity
{
    public partial class ProviderManager : IProviderManager
    {
        private readonly IComponentContext _ctx;
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;

        public ProviderManager(IComponentContext ctx, SmartDbContext db, ISettingService settingService)
        {
            _ctx = ctx;
            _db = db;
            _settingService = settingService;
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
                    if (d != null && !IsActiveForStore(d, storeId))
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
                    if (d != null && !IsActiveForStore(d, storeId))
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
                            where d == null || IsActiveForStore(d, storeId)
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
                            where d == null || IsActiveForStore(d, storeId)
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
            if (storeId == 0)
            {
                return true;
            }

            var limitedToStoresSetting = _settingService.GetSettingByKey<string>(module.GetSettingKey("LimitedToStores"));
            if (limitedToStoresSetting.IsEmpty())
            {
                return true;
            }

            var limitedToStores = limitedToStoresSetting.ToIntArray();
            if (limitedToStores.Length > 0)
            {
                var flag = limitedToStores.Contains(storeId);
                return flag;
            }

            return true;
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

        // TODO: (core) Move PluginMediator.ToProviderModel() to Controller/AdminControllerBase
    }
}
