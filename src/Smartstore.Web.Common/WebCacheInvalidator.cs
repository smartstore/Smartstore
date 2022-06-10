using Smartstore.Caching;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Core.Theming;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using Smartstore.Utilities;
using Smartstore.Web.Theming;

namespace Smartstore.Web
{
    internal partial class WebCacheInvalidator : AsyncDbSaveHook<BaseEntity>, IConsumer
    {
        #region Consts

        /// <summary>
        /// Key for tax display type caching
        /// </summary>
        /// <remarks>
        /// {0} : customer role ids
        /// {1} : store identifier
        /// </remarks>
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY = "web:customerroles:taxdisplaytypes-{0}-{1}";
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY = "web:customerroles:taxdisplaytypes*";

        #endregion

        private readonly ICacheManager _cache;
        private readonly ThemeVariableRepository _themeVarRepo;

        // Item1 = ThemeName, Item2 = StoreId
        private HashSet<Tuple<string, int>> _themeScopes;

        public WebCacheInvalidator(ICacheManager cache, ThemeVariableRepository themeVarRepo)
        {
            _cache = cache;
            _themeVarRepo = themeVarRepo;
        }

        public void HandleEvent(ThemeTouchedEvent eventMessage)
        {
            _themeVarRepo.RemoveFromCache(eventMessage.ThemeName);
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is ThemeVariable themeVar)
            {
                AddEvictableThemeScope(themeVar.Theme, themeVar.StoreId);
            }
            else if (entry.Entity is CustomerRole)
            {
                await _cache.RemoveByPatternAsync(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY);
            }
            else if (entry.Entity is Setting setting && entry.InitialState == EntityState.Modified)
            {
                if (setting.Name.EqualsNoCase(TypeHelper.NameOf<TaxSettings>(x => x.TaxDisplayType, true)))
                {
                    await _cache.RemoveByPatternAsync(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY); // depends on TaxSettings.TaxDisplayType
                }
            }
            else
            {
                return HookResult.Void;
            }

            return HookResult.Ok;
        }

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            FlushThemeVarsCacheEviction();
            return Task.CompletedTask;
        }

        #region Helpers

        private void AddEvictableThemeScope(string themeName, int storeId)
        {
            if (_themeScopes == null)
                _themeScopes = new HashSet<Tuple<string, int>>();
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
