using System.Text.RegularExpressions;
using Smartstore.Utilities;

namespace Smartstore.Caching
{
    public static class IRequestCacheExtensions
    {
        /// <summary>
        /// Removes all items whose keys match the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to match.</param>
        /// <returns>The number of removed cache items.</returns>
        public static int RemoveMany(this IRequestCache cache, Func<object, bool> predicate)
        {
            var items = cache.Items;
            var keysToRemove = cache.SelectKeys(predicate).ToArray();

            foreach (var key in keysToRemove)
            {
                items.Remove(key);
            }

            return keysToRemove.Length;
        }

        /// <summary>
        /// Removes all items whose string type keys match the given <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">
        ///     The string pattern to match.
        ///     <para>
        ///         Supported glob-style patterns:
        ///         - h?llo matches hello, hallo and hxllo
        ///         - h*llo matches hllo and heeeello
        ///         - h[ae]llo matches hello and hallo, but not hillo
        ///         - h[^e]llo matches hallo, hbllo, ... but not hello
        ///         - h[a-b]llo matches hallo and hbllo
        ///     </para>
        /// </param>
        /// <returns>The number of removed cache items.</returns>
        public static int RemoveByPattern(this IRequestCache cache, string pattern)
        {
            var items = cache.Items;
            var keysToRemove = cache.SelectKeys(pattern).ToArray();

            foreach (var key in keysToRemove)
            {
                items.Remove(key);
            }

            return keysToRemove.Length;
        }

        /// <summary>
        /// Returns all string type keys that match the given <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">
        ///     The string pattern to match.
        ///     <para>
        ///         Supported glob-style patterns:
        ///         - h?llo matches hello, hallo and hxllo
        ///         - h*llo matches hllo and heeeello
        ///         - h[ae]llo matches hello and hallo, but not hillo
        ///         - h[^e]llo matches hallo, hbllo, ... but not hello
        ///         - h[a-b]llo matches hallo and hbllo
        ///     </para>
        /// </param>
        /// <returns>List of matching keys.</returns>
        public static IEnumerable<object> SelectKeys(this IRequestCache cache, string pattern)
        {
            var wildcard = pattern.IsEmpty() || pattern == "*"
                ? null
                : new Wildcard(pattern, RegexOptions.IgnoreCase);

            return SelectKeys(cache, x => KeyMatcher(x, wildcard)).Cast<string>();

            static bool KeyMatcher(object key, Wildcard w)
            {
                if (key is string str)
                {
                    return w == null || w.IsMatch(str);
                }

                return false;
            }
        }

        /// <summary>
        /// Returns all keys that match the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="selector">The predicate to match.</param>
        /// <returns>List of matching keys.</returns>
        public static IEnumerable<object> SelectKeys(this IRequestCache cache, Func<object, bool> predicate)
        {
            Guard.NotNull(cache);
            Guard.NotNull(predicate);

            var items = cache.Items;
            if (items.Count == 0)
            {
                yield break;
            }

            foreach (var kvp in items)
            {
                if (predicate(kvp.Key))
                {
                    yield return kvp.Key;
                }
            }
        }
    }
}
