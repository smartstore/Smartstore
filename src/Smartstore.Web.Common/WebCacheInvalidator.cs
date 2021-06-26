using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Theming;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Utilities;
using Smartstore.Web.Bundling;
using Smartstore.Web.Theming;

namespace Smartstore.Web
{
    internal partial class WebCacheInvalidator : AsyncDbSaveHook<BaseEntity>, IConsumer
    {
        #region Consts

        /// <summary>
        /// Key for ThemeVariables caching
        /// </summary>
        /// <remarks>
        /// {0} : theme name
        /// {1} : store identifier
        /// </remarks>
        public const string THEMEVARS_KEY = "web:themevars-{0}-{1}";
        public const string THEMEVARS_THEME_KEY = "web:themevars-{0}";

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

        private readonly static ConcurrentDictionary<object, CancellationTokenSource> _themeVarCancelTokens = new();

        private readonly ICacheManager _cache;
        private readonly IMemoryCache _memCache;
        //private readonly IAssetCache _assetCache;

        // Item1 = ThemeName, Item2 = StoreId
        private HashSet<Tuple<string, int>> _themeScopes;

        public WebCacheInvalidator(ICacheManager cache, IMemoryCache memCache/*, IAssetCache assetCache*/)
        {
            _cache = cache;
            _memCache = memCache;
            //_assetCache = assetCache;
        }

        public void HandleEvent(ThemeTouchedEvent eventMessage)
        {
            var cacheKey = BuildThemeVarsCacheKey(eventMessage.ThemeName, 0) + "*";
            _memCache.RemoveByPattern(cacheKey);
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
                _memCache.Remove(BuildThemeVarsCacheKey(themeName, storeId));

                // Signal cancellation so that cached bundles can be busted from cache
                CancelThemeVarsToken(themeName, storeId);
            }

            _themeScopes.Clear();
        }

        internal static CancellationTokenSource GetThemeVarsToken(string theme, int storeId)
        {
            return GetThemeVarsToken(BuildThemeVarsCacheKey(theme, storeId));
        }

        internal static CancellationTokenSource GetThemeVarsToken(object cacheKey)
        {
            if (_themeVarCancelTokens.TryGetValue(cacheKey, out var cts))
            {
                return cts;
            }

            cts = new CancellationTokenSource();
            cts.Token.Register(() => _themeVarCancelTokens.TryRemove(cacheKey, out _));
            _themeVarCancelTokens.TryAdd(cacheKey, cts);
            return cts;
        }

        internal static void CancelThemeVarsToken(string theme, int storeId)
        {
            CancelThemeVarsToken(BuildThemeVarsCacheKey(theme, storeId));
        }

        internal static void CancelThemeVarsToken(object cacheKey)
        {
            if (_themeVarCancelTokens.TryRemove(cacheKey, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        internal static string BuildThemeVarsCacheKey(string theme, int storeId)
        {
            var memCache = EngineContext.Current.Application.Services.Resolve<IMemoryCache>();
            return BuildThemeVarsCacheKey(memCache, theme, storeId);
        }

        internal static string BuildThemeVarsCacheKey(IMemoryCache memCache, string theme, int storeId)
        {
            if (storeId > 0)
            {
                return memCache.BuildScopedKey(THEMEVARS_KEY.FormatInvariant(theme, storeId));
            }

            return memCache.BuildScopedKey(THEMEVARS_THEME_KEY.FormatInvariant(theme));
        }

        #endregion
    }
}
