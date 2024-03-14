using Smartstore.Caching;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Data;
using Smartstore.Events;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// Invalidates all menus that contain the <see cref="CatalogMenuProvider"/>
    /// </summary>
    internal class CatalogMenuInvalidator : IConsumer
    {
        private readonly IMenuService _menuService;
        private readonly CatalogSettings _catalogSettings;
        private readonly ICacheManager _cache;
        private readonly SmartDbContext _db;

        private List<string> _invalidated = new();
        private List<string> _countsResetted = new();

        public CatalogMenuInvalidator(
            IMenuService menuService,
            CatalogSettings catalogSettings,
            ICacheManager cache,
            SmartDbContext db)
        {
            _menuService = menuService;
            _catalogSettings = catalogSettings;
            _cache = cache;
            _db = db;
        }

        public async Task HandleAsync(CategoryTreeChangedEvent eventMessage)
        {
            var affectedMenuNames = await _db.MenuItems
                .AsNoTracking()
                .Where(x => x.ProviderName == "catalog")
                .Select(x => x.Menu.SystemName)
                .Distinct()
                .ToListAsync();

            foreach (var menuName in affectedMenuNames)
            {
                var reason = eventMessage.Reason;

                if (reason == CategoryTreeChangeReason.ElementCounts)
                {
                    await ResetElementCounts(menuName);
                }
                else
                {
                    await Invalidate(menuName);
                }
            }
        }

        private async Task Invalidate(string menuName)
        {
            if (!_invalidated.Contains(menuName))
            {
                await (await _menuService.GetMenuAsync(menuName))?.ClearCacheAsync();
                _invalidated.Add(menuName);
            }
        }

        private async Task ResetElementCounts(string menuName)
        {
            if (!_countsResetted.Contains(menuName) && _catalogSettings.ShowCategoryProductNumber)
            {
                var allCachedMenus = (await _menuService.GetMenuAsync(menuName))?.GetAllCachedMenus();
                if (allCachedMenus != null)
                {
                    foreach (var kvp in allCachedMenus)
                    {
                        bool dirty = false;
                        kvp.Value.Traverse(x =>
                        {
                            if (x.Value.ElementsCount.HasValue)
                            {
                                dirty = true;
                                x.Value.ElementsCount = null;
                                x.Value.ElementsCountResolved = false;
                            }
                        }, true);

                        if (dirty)
                        {
                            await _cache.PutAsync(kvp.Key, kvp.Value);
                        }
                    }
                }

                _countsResetted.Add(menuName);
            }
        }
    }
}
