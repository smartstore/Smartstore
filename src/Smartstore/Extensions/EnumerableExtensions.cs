#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;
using Smartstore.ComponentModel;
using Smartstore.Domain;

namespace Smartstore;

public static class EnumerableExtensions
{
    /// <summary>
    /// Checks whether given <paramref name="source"/> array is either <c>null</c> or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this T[]? source)
    {
        if (source == null)
        {
            return true;
        }

        return source.Length == 0;
    }

    /// <summary>
    /// Converts a set to a read-only set.
    /// </summary>
    public static ReadOnlySet<T> AsReadOnly<T>(this ISet<T> source)
    {
        Guard.NotNull(source);

        if (source.Count == 0)
        {
            return [];
        }

        return new ReadOnlySet<T>(source);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string StrJoin(this IEnumerable<string?> source, string? separator)
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
        return [.. source.Select(x => x.ToString())];
    }

    /// <summary>
    /// Orders a collection of entities by a specific ID sequence.
    /// </summary>
    public static IEnumerable<TEntity> OrderBySequence<TEntity>(this IEnumerable<TEntity> source, IEnumerable<int> ids)
        where TEntity : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(ids);

        var sorted = from id in ids
                     join entity in source on id equals entity.Id
                     select entity;

        return sorted;
    }

    extension<T>([NotNullWhen(false)] IEnumerable<T>? source)
    {
        /// <summary>
        /// Checks whether given <paramref name="source"/> collection is either <c>null</c> or empty.
        /// </summary>
        public bool IsNullOrEmpty()
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

        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (source == null)
            {
                return [];
            }
            else if (source is ReadOnlyCollection<T> readOnly)
            {
                return readOnly;
            }

            if (source.TryGetNonEnumeratedCount(out var count) && count == 0)
            {
                return [];
            }

            if (!source.Any())
            {
                return [];
            }

            if (source is List<T> list)
            {
                return list.AsReadOnly();
            }
            else if (source is IList<T> list2)
            {
                return new ReadOnlyCollection<T>(list2);
            }

            return new ReadOnlyCollection<T>([.. source]);
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Performs an action on each item while iterating through a list.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Each(Action<T> action)
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
        /// Performs an action on each item while iterating through a list with the index.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Each(Action<T, int> action)
        {
            int i = 0;
            foreach (T t in source)
            {
                action(t, i++);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<TKey, T> ToDictionarySafe<TKey>(Func<T, TKey> keySelector)
            where TKey : notnull
        {
            return source.ToDictionarySafe(keySelector, new Func<T, T>(src => src), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<TKey, T> ToDictionarySafe<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            return source.ToDictionarySafe(keySelector, new Func<T, T>(src => src), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<TKey, TElement> ToDictionarySafe<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
            where TKey : notnull
        {
            return source.ToDictionarySafe(keySelector, elementSelector, null);
        }

        public Dictionary<TKey, TElement> ToDictionarySafe<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            Guard.NotNull(source);
            Guard.NotNull(keySelector);
            Guard.NotNull(elementSelector);

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
        public TKey[] ToDistinctArray<TKey>(Func<T, TKey> elementSelector)
        {
            Guard.NotNull(source);
            Guard.NotNull(elementSelector);

            return source.Select(elementSelector).Distinct().ToArray();
        }

        /// <summary>
        /// Returns distinct elements by a key selector.
        /// </summary>
        public IEnumerable<T> DistinctBy<TKey>(Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            return source.Distinct(GenericEqualityComparer<T>.CompareMember(keySelector));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task EachAsync(Func<T, Task> action)
        {
            foreach (T t in source)
            {
                await action(t);
            }
        }

        public async Task EachAsync(Func<T, int, Task> action)
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
        public IAsyncEnumerable<T> WhereAwait(Func<T, Task<bool>> predicate)
        {
            return source.ToAsyncEnumerable().Where((x, ct) => new ValueTask<bool>(predicate(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form in parallel.
        /// </summary>
        public async Task<IEnumerable<TResult>> SelectAsyncParallel<TResult>(Func<T, Task<TResult>> selector)
        {
            return await Task.WhenAll(source.Select(async x => await selector(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form asynchronously.
        /// </summary>
        public IAsyncEnumerable<TResult> SelectAwait<TResult>(Func<T, Task<TResult>> selector)
        {
            return source.ToAsyncEnumerable().Select((x, i, ct) => new ValueTask<TResult>(selector(x)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form and flattens the resulting sequences into one sequence asynchronously.
        /// </summary>
        public async IAsyncEnumerable<TResult> SelectManyAwait<TResult>(Func<T, Task<IEnumerable<TResult>>> selector)
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
        public async Task<bool> AnyAsync(Func<T, Task<bool>> predicate)
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
    }
}
