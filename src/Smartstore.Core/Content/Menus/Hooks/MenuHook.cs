using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus.Hooks
{
    public class MenuHook : AsyncDbSaveHook<Menu>
    {
        private readonly SmartDbContext _db;
        private readonly IMenuStorage _menuStorage;
        private readonly ICacheManager _cache;
        private readonly HashSet<string> _toAdd = new();
        private readonly HashSet<string> _toRemove = new();

        public MenuHook(SmartDbContext db, IMenuStorage menuStorage, ICacheManager cache)
        {
            _db = db;
            _menuStorage = menuStorage;
            _cache = cache;
        }

        protected override Task<HookResult> OnInsertingAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Ensure valid system name.
            entity.SystemName = entity.SystemName.ToValidPath();
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Ensure valid system name.
            entity.SystemName = entity.SystemName.ToValidPath();

            var modProps = _db.GetModifiedProperties(entity);

            if (modProps.TryGetValue(nameof(entity.Published), out var original))
            {
                if (original.Convert<bool>() == true)
                {
                    _toRemove.Add(entity.SystemName);
                }
                else
                {
                    _toAdd.Add(entity.SystemName);
                }
            }
            else if (modProps.TryGetValue(nameof(entity.SystemName), out original))
            {
                _toRemove.Add((string)original);
                _toAdd.Add(entity.SystemName);
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertedAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.Published)
            {
                _toAdd.Add(entity.SystemName);
            }
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _toRemove.Add(entity.SystemName);
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var systemNames = await _menuStorage.GetMenuSystemNamesAsync(false);
            if (systemNames != null)
            {
                await systemNames.AddRangeAsync(_toAdd);
                await systemNames.ExceptWithAsync(_toRemove.ToArray());
            }

            _toAdd.Clear();
            _toRemove.Clear();

            await _cache.RemoveByPatternAsync(MenuStorage.MENU_PATTERN_KEY);
        }
    }
}
