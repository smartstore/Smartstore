using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus.Hooks
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    internal class MenuItemHook : AsyncDbSaveHook<MenuItemEntity>
    {
        private readonly ICacheManager _cache;

        public MenuItemHook(ICacheManager cache)
        {
            _cache = cache;
        }

        protected override Task<HookResult> OnInsertingAsync(MenuItemEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Prevent inconsistent tree structure.
            if (entity.ParentItemId != 0 && entity.ParentItemId == entity.Id)
            {
                entity.ParentItemId = 0;
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(MenuItemEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Prevent inconsistent tree structure.
            if (entity.ParentItemId != 0 && entity.ParentItemId == entity.Id)
            {
                entity.ParentItemId = 0;
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            await _cache.RemoveByPatternAsync(MenuStorage.MENU_PATTERN_KEY);
            return HookResult.Ok;
        }
    }
}
