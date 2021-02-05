using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus.Hooks
{
    [Important]
    public class MenuItemHook : AsyncDbSaveHook<MenuItem>
    {
        private readonly SmartDbContext _db;
        private readonly IMenuStorage _menuStorage;
        private readonly ICacheManager _cache;
        
        public MenuItemHook(SmartDbContext db, 
            IMenuStorage menuStorage,
            ICacheManager cache)
        {
            _db = db;
            _menuStorage = menuStorage;
            _cache = cache;
        }

        protected override Task<HookResult> OnInsertingAsync(MenuItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletingAsync(MenuItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = (MenuItem)entry.Entity;

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
