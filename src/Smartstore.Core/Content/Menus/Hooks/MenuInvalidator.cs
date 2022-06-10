using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus
{
    internal class MenuInvalidator : AsyncDbSaveHook<BaseEntity>
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IMenuService> _menuService;

        public MenuInvalidator(SmartDbContext db, Lazy<IMenuService> menuService)
        {
            _db = db;
            _menuService = menuService;
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is MenuEntity me)
            {
                await _menuService.Value.ClearCacheAsync(me.SystemName);
                return HookResult.Ok;
            }
            else if (entry.Entity is MenuItemEntity mie)
            {
                var menu = mie.Menu ?? await _db.Menus.FindByIdAsync(mie.MenuId, true, cancelToken);
                if (menu != null)
                {
                    await _menuService.Value.ClearCacheAsync(menu.SystemName);
                }
                return HookResult.Ok;
            }
            else
            {
                return HookResult.Void;
            }
        }
    }
}
