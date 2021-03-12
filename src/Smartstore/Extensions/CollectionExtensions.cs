using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Smartstore.Collections;

namespace Smartstore
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> initial, IEnumerable<T> other)
        {
            if (other == null)
                return;

            if (initial is List<T> list)
            {
                list.AddRange(other);
                return;
            }

            foreach (var local in other)
            {
                initial.Add(local);
            }
        }

        public static SyncedCollection<T> AsSynchronized<T>(this ICollection<T> source)
        {
            return AsSynchronized(source, new object());
        }

        public static SyncedCollection<T> AsSynchronized<T>(this ICollection<T> source, object syncRoot)
        {
            if (source is SyncedCollection<T> sc)
            {
                return sc;
            }

            return new SyncedCollection<T>(source, syncRoot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this ICollection<T> source)
        {
            return source == null || source.Count == 0;
        }

        /// <summary>
        /// Safe way to remove selected entries from a list.
        /// </summary>
        /// <remarks>To be used for materialized lists only, not IEnumerable or similar.</remarks>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="list">List.</param>
        /// <param name="selector">Selector for the entries to be removed.</param>
        /// <returns>Number of removed entries.</returns>
        public static int Remove<T>(this IList<T> list, Func<T, bool> selector)
        {
            Guard.NotNull(list, nameof(list));
            Guard.NotNull(selector, nameof(selector));

            var count = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (selector(list[i]))
                {
                    list.RemoveAt(i);
                    ++count;
                }
            }

            return count;
        }
    }
}
