using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public partial class MenuStorage : IMenuStorage
    {
        private const string MENU_ALLSYSTEMNAMES_CACHE_KEY = "MenuStorage:SystemNames";
        private const string MENU_USER_CACHE_KEY = "MenuStorage:Menus:User-{0}-{1}";
        internal const string MENU_PATTERN_KEY = "MenuStorage:Menus:*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public MenuStorage(SmartDbContext db,
            ICacheManager cache,
            IWorkContext workContext,
            IStoreContext storeContext)
        {
            _db = db;
            _cache = cache;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public virtual async Task<IEnumerable<MenuInfo>> GetUserMenuInfosAsync(IEnumerable<CustomerRole> roles = null, int storeId = 0)
        {
            if (roles == null)
            {
                roles = _workContext.CurrentCustomer.CustomerRoleMappings.Select(x => x.CustomerRole);
            }

            if (storeId == 0)
            {
                storeId = _storeContext.CurrentStore.Id;
            }

            var roleIds = roles.Where(x => x.Active).Select(x => x.Id);
            var cacheKey = MENU_USER_CACHE_KEY.FormatInvariant(storeId, string.Join(',', roleIds));

            var userMenusInfo = await _cache.GetAsync(cacheKey, async () =>
            {
                var query = _db.Menus
                    .AsNoTracking()
                    .ApplyStandardFilter(null, false, storeId, roleIds.ToArray())
                    .ApplySorting();

                var data = await query.Select(x => new
                {
                    x.Id,
                    x.SystemName,
                    x.Template,
                    x.WidgetZone,
                    x.DisplayOrder
                })
                .ToListAsync();

                var result = data.Select(x => new MenuInfo
                {
                    Id = x.Id,
                    SystemName = x.SystemName,
                    Template = x.Template,
                    DisplayOrder = x.DisplayOrder,
                    WidgetZones = x.WidgetZone.EmptyNull().Trim()
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(y => y.Trim())
                        .ToArray()
                })
                .ToList();

                return result;
            });

            return userMenusInfo;
        }

        public virtual async Task<bool> MenuExistsAsync(string systemName)
        {
            if (systemName.IsEmpty())
            {
                return false;
            }

            return (await GetMenuSystemNamesAsync(true)).Contains(systemName);
        }

        public virtual async Task<ISet> GetMenuSystemNamesAsync(bool ensureCreated)
        {
            if (ensureCreated || await _cache.ContainsAsync(MENU_ALLSYSTEMNAMES_CACHE_KEY))
            {
                return await _cache.GetHashSetAsync(MENU_ALLSYSTEMNAMES_CACHE_KEY, async () =>
                {
                    return await _db.Menus
                        .AsNoTracking()
                        .Where(x => x.Published)
                        .OrderByDescending(x => x.IsSystemMenu)
                        .ThenBy(x => x.Id)
                        .Select(x => x.SystemName)
                        .ToArrayAsync();
                });
            }

            return null;
        }

        public virtual async Task DeleteMenuItemAsync(MenuItemEntity item, bool deleteChilds = true)
        {
            if (item == null)
            {
                return;
            }

            if (!deleteChilds)
            {
                _db.MenuItems.Remove(item);

                // INFO: let hook invalidate cache
                await _db.SaveChangesAsync();
            }
            else
            {
                var ids = new HashSet<int> { item.Id };
                await GetChildIdsAsync(item.Id, ids);

                foreach (var chunk in ids.Chunk(200))
                {
                    var items = await _db.MenuItems
                        .Where(x => chunk.Contains(x.Id))
                        .ExecuteDeleteAsync();
                }

                // INFO: No hook will run. Invalidate cache manually.
                await _cache.RemoveByPatternAsync(MENU_PATTERN_KEY);
            }

            async Task GetChildIdsAsync(int parentId, HashSet<int> ids)
            {
                var childIds = await _db.MenuItems
                    .AsNoTracking()
                    .Where(x => x.ParentItemId == parentId)
                    .Select(x => x.Id)
                    .ToArrayAsync();

                if (childIds.Any())
                {
                    ids.AddRange(childIds);
                    await childIds.EachAsync(x => GetChildIdsAsync(x, ids));
                }
            }
        }
    }
}
