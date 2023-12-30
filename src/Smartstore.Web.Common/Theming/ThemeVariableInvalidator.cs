using Smartstore.Core.Theming;
using Smartstore.Data.Hooks;

namespace Smartstore.Web.Theming
{
    internal partial class ThemeVariableInvalidator : AsyncDbSaveHook<ThemeVariable>
    {
        private readonly ThemeVariableRepository _themeVarRepo;

        // Item1 = ThemeName, Item2 = StoreId
        private HashSet<Tuple<string, int>> _themeScopes;

        public ThemeVariableInvalidator(ThemeVariableRepository themeVarRepo)
        {
            _themeVarRepo = themeVarRepo;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is ThemeVariable themeVar)
            {
                AddEvictableThemeScope(themeVar.Theme, themeVar.StoreId);
                return Task.FromResult(HookResult.Ok);
            }

            return Task.FromResult(HookResult.Void);
        }

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            FlushThemeVarsCacheEviction();
            return Task.CompletedTask;
        }

        #region Helpers

        private void AddEvictableThemeScope(string themeName, int storeId)
        {
             _themeScopes ??= [];
            _themeScopes.Add(new Tuple<string, int>(themeName, storeId));
        }

        private void FlushThemeVarsCacheEviction()
        {
            if (_themeScopes == null || _themeScopes.Count == 0)
                return;

            foreach (var scope in _themeScopes)
            {
                var themeName = scope.Item1;
                var storeId = scope.Item2;

                // Remove theme vars from cache
                _themeVarRepo.RemoveFromCache(themeName, storeId);
            }

            _themeScopes.Clear();
        }

        #endregion
    }
}
