using Smartstore.Collections;

namespace Smartstore;

public static class CollectionExtensions
{
    extension<T>(ICollection<T> source)
    {
        public void AddRange(IEnumerable<T> other)
        {
            if (other == null)
                return;

            if (source is List<T> list)
            {
                list.AddRange(other);
                return;
            }

            foreach (var local in other)
            {
                source.Add(local);
            }
        }

        public SyncedCollection<T> AsSynchronized()
        {
            if (source is SyncedCollection<T> sc)
            {
                return sc;
            }

            return new SyncedCollection<T>(source);
        }
    }

    extension<T>(IList<T> list)
    {
        /// <summary>
        /// Safe way to remove selected entries from a list.
        /// </summary>
        /// <remarks>To be used for materialized lists only, not IEnumerable or similar.</remarks>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="selector">Selector for the entries to be removed.</param>
        /// <returns>Number of removed entries.</returns>
        public int Remove(Func<T, bool> selector)
        {
            Guard.NotNull(list);
            Guard.NotNull(selector);

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

    extension<T>(Stack<T> stack)
    {
        public bool TryPeek(out T value)
        {
            value = default;

            if (stack.Count > 0)
            {
                value = stack.Peek();
                return true;
            }

            return false;
        }

        public bool TryPop(out T value)
        {
            value = default;

            if (stack.Count > 0)
            {
                value = stack.Pop();
                return true;
            }

            return false;
        }
    }
}
