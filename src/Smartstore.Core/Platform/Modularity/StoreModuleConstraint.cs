using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;

namespace Smartstore.Engine.Modularity
{
    public class StoreModuleConstraint : IModuleConstraint
    {
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;

        public StoreModuleConstraint(IStoreContext storeContext, ISettingService settingService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
        }

        public bool Matches(IModuleDescriptor descriptor, int? storeId)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            if (storeId == null)
            {
                storeId = _storeContext.CurrentStore.Id;
            }

            if (storeId == 0)
            {
                return true;
            }

            var limitedToStoresSetting = _settingService.GetSettingByKey<string>(descriptor.GetSettingKey("LimitedToStores"));
            if (limitedToStoresSetting.IsEmpty())
            {
                return true;
            }

            var limitedToStores = limitedToStoresSetting.ToIntArray();
            if (limitedToStores.Length > 0)
            {
                return limitedToStores.Contains(storeId.Value);
            }

            return true;
        }
    }
}
