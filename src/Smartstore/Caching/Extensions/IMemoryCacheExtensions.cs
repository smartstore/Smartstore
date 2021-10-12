using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class IMemoryCacheExtensions
    {
        const string CacheRegionName = "Smartstore:";

        private readonly static FieldInfo _entriesFieldInfo = typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Build a scoped memory cache key by simply prepending "Smartstore:" to the given key.
        /// </summary>
        public static string BuildScopedKey(this IMemoryCache _, string key)
        {
            return key.HasValue() ? CacheRegionName + key : null;
        }

        /// <summary>
        /// Scans for all keys matching the input pattern.
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">A key pattern. Can be <c>null</c>.</param>
        /// <returns>A list of matching key names</returns>
        public static IEnumerable<object> EnumerateKeys(this IMemoryCache cache, string pattern = "*")
        {
            Guard.NotNull(cache, nameof(cache));
            
            var allKeys = (_entriesFieldInfo.GetValue(cache) as IDictionary).Keys
                .Cast<object>()
                .AsParallel();

            if (pattern.IsEmpty() || pattern == "*")
            {
                return allKeys;
            }

            var wildcard = new Wildcard(pattern, RegexOptions.IgnoreCase);
            return allKeys.OfType<string>().Where(x => wildcard.IsMatch(x));
        }

        /// <summary>
        /// Removes all entries with keys matching the input pattern.
        /// <para>
        ///     Supported glob-style patterns:
        ///     - h?llo matches hello, hallo and hxllo
        ///     - h*llo matches hllo and heeeello
        ///     - h[ae]llo matches hello and hallo, but not hillo
        ///     - h[^e]llo matches hallo, hbllo, ... but not hello
        ///     - h[a-b]llo matches hallo and hbllo
        /// </para>
        /// </summary>
        /// <param name="pattern">Glob pattern</param>
        /// <returns>Number of removed cache items</returns>
        public static int RemoveByPattern(this IMemoryCache cache, string pattern = "*")
        {
            Guard.NotNull(cache, nameof(cache));
            
            var keysToRemove = EnumerateKeys(cache, pattern).ToArray();
            int numRemoved = 0;

            foreach (string key in keysToRemove)
            {
                cache.Remove(key);
                numRemoved++;
            }

            return numRemoved;
        }
    }
}
