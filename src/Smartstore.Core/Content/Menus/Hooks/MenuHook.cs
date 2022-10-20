using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;
using Smartstore.IO;

namespace Smartstore.Core.Content.Menus.Hooks
{
    internal class MenuHook : AsyncDbSaveHook<MenuEntity>
    {
        private readonly SmartDbContext _db;
        private readonly IMenuStorage _menuStorage;
        private readonly ICacheManager _cache;
        private readonly HashSet<string> _toAdd = new();
        private readonly HashSet<string> _toRemove = new();
        private string _hookErrorMessage;

        public MenuHook(SmartDbContext db, IMenuStorage menuStorage, ICacheManager cache)
        {
            _db = db;
            _menuStorage = menuStorage;
            _cache = cache;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override Task<HookResult> OnInsertingAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Ensure valid system name.
            entity.SystemName = PathUtility.SanitizePath(entity.SystemName);
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Ensure valid system name.
            entity.SystemName = PathUtility.SanitizePath(entity.SystemName);

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

        protected override Task<HookResult> OnDeletingAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.IsSystemMenu)
            {
                // Cannot delete the system menu.
                entry.ResetState();
                _hookErrorMessage = T("Admin.ContentManagement.Menus.CannotBeDeleted", entity.SystemName.NaIfEmpty());
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertedAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.Published)
            {
                _toAdd.Add(entity.SystemName);
            }
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatedAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(MenuEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _toRemove.Add(entity.SystemName);
            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
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
