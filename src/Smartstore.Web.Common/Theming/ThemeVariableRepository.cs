using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Core.Theming;
using Smartstore.Utilities;

namespace Smartstore.Web.Theming
{
    public partial class ThemeVariableRepository
    {
        #region GeneratedRegex

        [GeneratedRegex("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled)]
        private static partial Regex KeyWhitelistRegex();

        [GeneratedRegex("[:;]+", RegexOptions.Compiled)]
        private static partial Regex ValueBlacklistRegex();

        [GeneratedRegex("[$][a-zA-Z0-9_-]+", RegexOptions.Compiled)]
        private static partial Regex ValueSassVarsRegex();

        #endregion

        const string SassVarPrefix = "$";

        /// <summary>
        /// Key for ThemeVariables caching
        /// </summary>
        /// <remarks>
        /// {0} : theme name
        /// {1} : store identifier
        /// </remarks>
        const string THEMEVARS_KEY = "web:themevars-{0}-{1}";
        const string THEMEVARS_THEME_KEY = "web:themevars-{0}";

        private static readonly Regex _keyWhitelist = KeyWhitelistRegex();
        private static readonly Regex _valueBlacklist = ValueBlacklistRegex();
        private static readonly Regex _valueSassVars = ValueSassVarsRegex();
        //private static readonly Regex s_valueWhitelist = new Regex(@"^[#@]?[a-zA-Z0-9""' _\.,-]*$");

        private static readonly ConcurrentDictionary<object, CancellationTokenSource> _cancelTokens = new();

        private readonly IThemeVariableService _themeVarService;
        private readonly IMemoryCache _memCache;

        public ThemeVariableRepository(IThemeVariableService themeVarService, IMemoryCache memCache)
        {
            _themeVarService = themeVarService;
            _memCache = memCache;
        }

        #region Static (Cache and CancellationToken)

        private static string BuildCacheKey(IMemoryCache cache, string theme, int storeId)
        {
            if (storeId > 0)
            {
                return cache.BuildScopedKey(THEMEVARS_KEY.FormatInvariant(theme, storeId));
            }

            return cache.BuildScopedKey(THEMEVARS_THEME_KEY.FormatInvariant(theme));
        }

        private static string BuildTokenKey(string theme, int storeId)
            => $"ThemeVarToken:{theme}:{storeId}";

        internal static CancellationTokenSource GetToken(string theme, int storeId)
            => GetToken(BuildTokenKey(theme, storeId));

        private static CancellationTokenSource GetToken(object tokenKey)
        {
            if (_cancelTokens.TryGetValue(tokenKey, out var cts))
            {
                return cts;
            }

            cts = new CancellationTokenSource();
            cts.Token.Register(() => _cancelTokens.TryRemove(tokenKey, out _));
            _cancelTokens.TryAdd(tokenKey, cts);
            return cts;
        }

        private static void OnPostEviction(object key, object value, EvictionReason reason, object state)
        {
            // Signal cancellation so that cached bundles can be busted from cache
            CancelToken(state);
        }

        internal static void CancelToken(string theme, int storeId)
        {
            CancelToken(BuildTokenKey(theme, storeId));
        }

        private static void CancelToken(object tokenKey)
        {
            if (_cancelTokens.TryRemove(tokenKey, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        #endregion

        public async Task<string> GetPreprocessorCssAsync(string themeName, int storeId)
        {
            Guard.NotEmpty(themeName);
            Guard.IsPositive(storeId);

            var rawVars = await GetRawVariablesAsync(themeName, storeId);
            var variables = BuildVariables(rawVars);
            var css = GenerateSass(variables);

            return css;
        }

        internal IDictionary<string, string> BuildVariables(IDictionary<string, object> rawVariables)
        {
            Guard.NotNull(rawVariables, nameof(rawVariables));

            var result = new Dictionary<string, string>();

            foreach (var v in rawVariables)
            {
                string key = v.Key;

                if (v.Value == null || !_keyWhitelist.IsMatch(key))
                    continue;

                string value = v.Value.ToString();

                if (_valueBlacklist.IsMatch(value))
                    continue;

                //if (!s_valueWhitelist.IsMatch(value))
                //    continue;

                result.Add(key, value);
            }

            return result;
        }

        public async Task<IDictionary<string, object>> GetRawVariablesAsync(string themeName, int storeId)
        {
            var cacheKey = BuildCacheKey(_memCache, themeName, storeId);
            var tokenKey = BuildTokenKey(themeName, storeId);

            return await _memCache.GetOrCreateAsync(cacheKey, async (entry) =>
            {
                // Ensure that when this item is expired, any bundle depending on the token is also expired
                entry.RegisterPostEvictionCallback(OnPostEviction, tokenKey);

                return await GetRawVariablesCoreAsync(themeName, storeId);
            });
        }

        private async Task<IDictionary<string, object>> GetRawVariablesCoreAsync(string themeName, int storeId)
        {
            return await _themeVarService.GetThemeVariablesAsync(themeName, storeId) ?? new ExpandoObject();
        }

        public void RemoveFromCache(string themeName, int storeId = 0)
            => RemoveFromCache(_memCache, themeName, storeId);

        internal static void RemoveFromCache(IMemoryCache cache, string themeName, int storeId = 0)
        {
            Guard.NotNull(cache);
            Guard.NotEmpty(themeName);

            var cacheKey = BuildCacheKey(cache, themeName, storeId);

            if (storeId > 0)
            {
                cache.Remove(cacheKey);
            }
            else
            {
                cache.RemoveByPattern(cacheKey + "*");
            }
        }

        internal static string GenerateSass(IDictionary<string, string> variables)
        {
            if (variables == null || variables.Count == 0)
                return string.Empty;

            var prefix = SassVarPrefix;

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            foreach (var parameter in variables.Where(kvp => kvp.Value.HasValue()))
            {
                sb.AppendFormat("{0}{1}: {2};\n", prefix, parameter.Key, parameter.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks whether the passed SASS value is a valid, displayable HTML color,
        /// e.g.: "lighten($red, 20%)" would return <c>false</c>.
        /// </summary>
        /// <param name="value">The SASS value to test</param>
        /// <remarks>
        /// We need this helper during theme configuration: we just can't render
        /// color pickers for color values containing var references or SASS functions.
        /// </remarks>
        public static bool IsValidColor(string value)
        {
            if (value.IsEmpty())
            {
                return true;
            }

            if (_valueSassVars.IsMatch(value))
            {
                return false;
            }

            if (
                value[0] == '#' || 
                value.StartsWith("rgb(") || 
                value.StartsWith("rgba(") ||
                value.StartsWith("var(--") || // Allow CSS vars (because of swatches)
                value.StartsWith("hsl(") || 
                value.StartsWith("hsla("))
            {
                return true;
            }

            // Let pass all color names (red, blue etc.), but reject functions, e.g. "lighten(#fff, 10%)"
            return !value.Contains("(");
        }
    }
}
