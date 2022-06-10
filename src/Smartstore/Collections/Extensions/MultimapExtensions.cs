using System.Runtime.CompilerServices;
using Smartstore.Collections;

namespace Smartstore
{
    public static class MultimapExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Multimap<TKey, TValue> ToMultimap<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector)
        {
            return source.ToMultimap(keySelector, valueSelector, null);
        }

        public static Multimap<TKey, TValue> ToMultimap<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (valueSelector == null)
                throw new ArgumentNullException(nameof(valueSelector));

            var map = new Multimap<TKey, TValue>(comparer);

            foreach (var item in source)
            {
                map.Add(keySelector(item), valueSelector(item));
            }

            return map;
        }
    }
}
