using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public partial class MenuStorage : IMenuStorage
    {
        private const string MenuAllSystemNamesCacheKey = "MenuStorage:Menus:SystemNames-{0}";
        private const string MenuUserCacheKey = "MenuStorage:Menus:User-{0}-{1}";
        internal const string MenuPatternKey = "MenuStorage:Menus:*";

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

        public virtual async Task<IEnumerable<MenuInfo>> GetUserMenuInfosAsync(IEnumerable<CustomerRole> roles = null, int? storeId = null)
        {
            storeId = Guard.NotZero(storeId ?? _storeContext.CurrentStore.Id);
            roles ??= _workContext.CurrentCustomer.CustomerRoleMappings.Select(x => x.CustomerRole);

            var roleIds = roles.Where(x => x.Active).Select(x => x.Id);
            var cacheKey = MenuUserCacheKey.FormatInvariant(storeId, string.Join(',', roleIds));

            var userMenusInfo = await _cache.GetAsync(cacheKey, async () =>
            {
                var query = _db.Menus
                    .AsNoTracking()
                    .ApplyStandardFilter(null, false, storeId.Value, roleIds.ToArray())
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
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
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

        public virtual async Task<ISet> GetMenuSystemNamesAsync(bool ensureCreated, int? storeId = null)
        {
            storeId = Guard.NotZero(storeId ?? _storeContext.CurrentStore.Id);

            var key = MenuAllSystemNamesCacheKey.FormatInvariant(storeId.Value);

            if (ensureCreated || await _cache.ContainsAsync(key))
            {
                return await _cache.GetHashSetAsync(key, async () =>
                {
                    var systemNames = await _db.Menus
                        .AsNoTracking()
                        .Where(x => x.Published)
                        .ApplyStoreFilter(storeId.Value)
                        .OrderByDescending(x => x.IsSystemMenu)
                        .ThenBy(x => x.Id)
                        .Select(x => x.SystemName)
                        .ToArrayAsync();

                    // INFO: Distinct after materialization to keep the sorting and avoid EF warning.
                    return systemNames.Distinct();
                }, preserveOrder: true);
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
                await _cache.RemoveByPatternAsync(MenuPatternKey);
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
