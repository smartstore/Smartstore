using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;
using Smartstore.ComponentModel;
using Smartstore.Domain;

namespace Smartstore
{
    public static class EnumerableExtensions
    {
        #region Nested classes

        private static class DefaultReadOnlyCollection<T>
        {
            private static ReadOnlyCollection<T> defaultCollection;

            [SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingExpression")]
            internal static ReadOnlyCollection<T> Empty
            {
                get
                {
                    if (defaultCollection == null)
                    {
                        defaultCollection = new ReadOnlyCollection<T>(new T[0]);
                    }
                    return defaultCollection;
                }
            }
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Checks whether given <paramref name="source"/> collection is either
        /// <c>null</c> or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                return true;
            }

            if (source.TryGetNonEnumeratedCount(out var count))
            {
                return count == 0;
            }

            return !source.Any();
        }

        /// <summary>
        /// Performs an action on each item while iterating through a list. 
        /// This is a handy shortcut for <c>foreach(item in list) { ... }</c>
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The list, which holds the objects.</param>
        /// <param name="action">The action delegate which is called on each item while iterating.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source is List<T> list)
            {
                list.ForEach(action);
                return;
            }

            foreach (T t in source)
            {
                action(t);
            }
        }

        /// <summary>
        /// Performs an action on each item while iterating through a list. 
        /// This is a handy shortcut for <c>foreach(item in list) { ... }</c>
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The list, which holds the objects.</param>
        /// <param name="action">The action delegate which is called on each item while iterating.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Each<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int i = 0;
            foreach (T t in source)
            {
                action(t, i++);
            }
        }

        public static ReadOnlyCollection<T> AsReadOnly<T>(this IEnumerable<T> source)
        {
            if (source == null || !source.Any())
            {
                return DefaultReadOnlyCollection<T>.Empty;
            }

            if (source is ReadOnlyCollection<T> readOnly)
            {
                return readOnly;
            }
            else if (source is List<T> list)
            {
                return list.AsReadOnly();
            }
            else if (source is IList<T> list2)
            {
                return new ReadOnlyCollection<T>(list2);
            }

            return new ReadOnlyCollection<T>(source.ToList());
        }

        /// <summary>
        /// Converts an enumerable to a dictionary while tolerating duplicate entries (last wins)
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="keySelector">keySelector</param>
        /// <returns>Result as dictionary</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TSource> ToDictionarySafe<TSource, TKey>(
            this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector)
        {
            return source.ToDictionarySafe(keySelector, new Func<TSource, TSource>(src => src), null);
        }

        /// <summary>
        /// Converts an enumerable to a dictionary while tolerating duplicate entries (last wins)
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="keySelector">keySelector</param>
        /// <param name="comparer">comparer</param>
        /// <returns>Result as dictionary</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TSource> ToDictionarySafe<TSource, TKey>(
            this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector,
             IEqualityComparer<TKey> comparer)
        {
            return source.ToDictionarySafe(keySelector, new Func<TSource, TSource>(src => src), comparer);
        }

        /// <summary>
        /// Converts an enumerable to a dictionary while tolerating duplicate entries (last wins)
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="keySelector">keySelector</param>
        /// <param name="elementSelector">elementSelector</param>
        /// <returns>Result as dictionary</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TElement> ToDictionarySafe<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector,
             Func<TSource, TElement> elementSelector)
        {
            return source.ToDictionarySafe(keySelector, elementSelector, null);
        }

        /// <summary>
        /// Converts an enumerable to a dictionary while tolerating duplicate entries (last wins)
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="keySelector">keySelector</param>
        /// <param name="elementSelector">elementSelector</param>
        /// <param name="comparer">comparer</param>
        /// <returns>Result as dictionary</returns>
        public static Dictionary<TKey, TElement> ToDictionarySafe<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector,
             Func<TSource, TElement> elementSelector,
             IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (elementSelector == null)
            {
                throw new ArgumentNullException(nameof(elementSelector));
            }

            var dictionary = new Dictionary<TKey, TElement>(comparer);

            foreach (var local in source)
            {
                dictionary[keySelector(local)] = elementSelector(local);
            }

            return dictionary;
        }

        /// <summary>
        /// Selects elements of an enumerable and converts them into an array of distinct elements.
        /// </summary>
        /// <param name="source">Source enumerable.</param>
        /// <param name="elementSelector">Element selector.</param>
        /// <returns>Array of distinct elements.</returns>
        public static TKey[] ToDistinctArray<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> elementSelector)
        {
            Guard.NotNull(source);
            Guard.NotNull(elementSelector);

            return source.Select(elementSelector).Distinct().ToArray();
        }

        /// <summary>The distinct by.</summary>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <returns>the unique list</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            return source.Distinct(GenericEqualityComparer<TSource>.CompareMember(keySelector));
        }

        /// <summary>
        /// Orders a collection of entities by a specific ID sequence
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="source">The entity collection to sort</param>
        /// <param name="ids">The IDs to order by</param>
        /// <returns>The sorted entity collection</returns>
        public static IEnumerable<TEntity> OrderBySequence<TEntity>(this IEnumerable<TEntity> source, IEnumerable<int> ids)
            where TEntity : BaseEntity
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var sorted = from id in ids
                         join entity in source on id equals entity.Id
                         select entity;

            return sorted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StrJoin(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StrJoin(this IEnumerable<string> source, char separator)
        {
            return string.Join(separator, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] ToStringArray(this IEnumerable<StringSegment> source)
        {
            return source.Select(x => x.ToString()).ToArray();
        }

        #endregion

        #region Async

        /// <summary>
        /// Performs an action on each item while iterating through a list. 
        /// This is a handy shortcut for <c>foreach(item in list) { await ... }</c>
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The list, which holds the objects.</param>
        /// <param name="action">The action delegate which is called on each item while iterating.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task EachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            foreach (T t in source)
            {
                await action(t);
            }
        }

        /// <summary>
        /// Performs an action on each item while iterating through a list. 
        /// This is a handy shortcut for <c>foreach(item in list) { ... }</c>
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The list, which holds the objects.</param>
        /// <param name="action">The action delegate which is called on each item while iterating.</param>
        public static async Task EachAsync<T>(this IEnumerable<T> source, Func<T, int, Task> action)
        {
            int i = 0;
            foreach (T t in source)
            {
                await action(t, i++);
            }
        }

        /// <summary>
        /// Filters a sequence of values based on an async predicate.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to filter.</param>
        /// <param name="predicate">An async task function to test each element for a condition.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        public static IAsyncEnumerable<T> WhereAwait<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            return source.ToAsyncEnumerable().WhereAwait(x => new ValueTask<bool>(predicate(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form in parallel.
        /// </summary>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static async Task<IEnumerable<TResult>> SelectAsyncParallel<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            return await Task.WhenAll(source.Select(async x => await selector(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form asynchronously.
        /// </summary>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            return source.ToAsyncEnumerable().SelectAwait(x => new ValueTask<TResult>(selector(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form and flattens the resulting sequences into one sequence asynchronously.
        /// </summary>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static async IAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<IEnumerable<TResult>>> selector)
        {
            await foreach (var item in source.ToAsyncEnumerable())
            {
                var manyItems = await selector(item);
                foreach (var subItem in manyItems)
                {
                    yield return subItem;
                }
            }
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition asynchronously. 
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The source sequence whose elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        public static async Task<bool> AnyAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            foreach (T t in source)
            {
                if (await predicate(t))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
