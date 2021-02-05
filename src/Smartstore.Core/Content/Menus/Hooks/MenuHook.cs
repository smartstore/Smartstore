using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus.Hooks
{
    [Important]
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

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = (Menu)entry.Entity;

            // Ensure valid system name.
            entity.SystemName = entity.SystemName.ToValidPath();

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertedAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatingAsync(Menu entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var modProps = _db.GetModifiedProperties(entity);

            if (entry.State == Smartstore.Data.EntityState.Added && entity.Published)
            {
                _toAdd.Add(entity.SystemName);
            }
            if (entry.State == Smartstore.Data.EntityState.Deleted && entity.Published)
            {
                _toRemove.Add(entity.SystemName);
            }
            else if (entry.State == Smartstore.Data.EntityState.Modified)
            {
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
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var systemNames = await _menuStorage.GetMenuSystemNamesAsync(false);
            if (systemNames != null)
            {
                await systemNames.AddRangeAsync(_toAdd);
                _toAdd.Clear();

                await systemNames.ExceptWithAsync(_toRemove.ToArray());
                _toRemove.Clear();
            }

            await _cache.RemoveByPatternAsync(MenuStorage.MENU_PATTERN_KEY);
        }
    }
}
