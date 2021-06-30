using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Core.Theming;
using Smartstore.Utilities;

namespace Smartstore.Web.Theming
{
    public class ThemeVariableRepository
    {
        const string SassVarPrefix = "$";

        private static readonly Regex _keyWhitelist = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex _valueBlacklist = new Regex(@"[:;]+", RegexOptions.Compiled);
        private static readonly Regex _valueSassVars = new Regex(@"[$][a-zA-Z0-9_-]+", RegexOptions.Compiled);
        //private static readonly Regex s_valueWhitelist = new Regex(@"^[#@]?[a-zA-Z0-9""' _\.,-]*$");

        private readonly IThemeVariableService _themeVarService;
        private readonly IMemoryCache _memCache;

        public ThemeVariableRepository(IThemeVariableService themeVarService, IMemoryCache memCache)
        {
            _themeVarService = Guard.NotNull(themeVarService, nameof(themeVarService));
            _memCache = Guard.NotNull(memCache, nameof(memCache));
        }

        public async Task<string> GetPreprocessorCssAsync(string themeName, int storeId)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            Guard.IsPositive(storeId, nameof(storeId));

            var variables = await GetVariablesAsync(themeName, storeId);
            var css = Transform(variables);

            return css;
        }

        private async Task<IDictionary<string, string>> GetVariablesAsync(string themeName, int storeId)
        {
            var result = new Dictionary<string, string>();

            var rawVars = await GetRawVariablesAsync(themeName, storeId);

            foreach (var v in rawVars)
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

        public virtual async Task<ExpandoObject> GetRawVariablesAsync(string themeName, int storeId, bool skipCache = false)
        {
            // TODO: (core) "skipCache" somehow replaces ThemeHelper.IsStyleValidationRequest(). Change the callers accordingly.

            if (skipCache)
            {
                // Return uncached fresh data (the variables is not nuked yet)
                return await GetRawVariablesCoreAsync(themeName, storeId);
            }
            else
            {
                string cacheKey = WebCacheInvalidator.BuildThemeVarsCacheKey(themeName, storeId);
                return await _memCache.GetOrCreateAsync(cacheKey, async (entry) => 
                {
                    // Ensure that when this item is expired, any bundle depending on the token is also expired
                    entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        WebCacheInvalidator.CancelThemeVarsToken(key);
                    });

                    return await GetRawVariablesCoreAsync(themeName, storeId);
                });
            }
        }

        private async Task<ExpandoObject> GetRawVariablesCoreAsync(string themeName, int storeId)
        {
            return (await _themeVarService.GetThemeVariablesAsync(themeName, storeId)) ?? new ExpandoObject();
        }

        private static string Transform(IDictionary<string, string> parameters)
        {
            if (parameters.Count == 0)
                return string.Empty;

            var prefix = SassVarPrefix;

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            foreach (var parameter in parameters.Where(kvp => kvp.Value.HasValue()))
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
        internal static bool IsValidColor(string value)
        {
            if (value.IsEmpty())
            {
                return true;
            }

            if (_valueSassVars.IsMatch(value))
            {
                return false;
            }

            if (value[0] == '#' || value.StartsWith("rgb(") || value.StartsWith("rgba(") || value.StartsWith("hsl(") || value.StartsWith("hsla("))
            {
                return true;
            }

            // Let pass all color names (red, blue etc.), but reject functions, e.g. "lighten(#fff, 10%)"
            return !value.Contains("(");
        }
    }
}
