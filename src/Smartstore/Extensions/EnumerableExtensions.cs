using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.Extensions.Primitives;
using Smartstore.ComponentModel;
using Smartstore.Domain;
using Smartstore.Extensions.Internal;

namespace Smartstore
{
    public static class CollectionSlicer
	{
		/// <summary>
		/// Slices the iteration over an enumerable by the given slice sizes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The source sequence to slice</param>
		/// <param name="sizes">
		/// Slice sizes. At least one size is required. Multiple sizes result in differently sized slices,
		/// whereat the last size is used for the "rest" (if any)
		/// </param>
		/// <returns>The sliced enumerable</returns>
		public static IEnumerable<IEnumerable<T>> Slice<T>(this IEnumerable<T> source, params int[] sizes)
		{
			if (!sizes.Any(step => step != 0))
			{
				throw new InvalidOperationException("Can't slice a collection with step length 0.");
			}

			return new EnumerableSlicer<T>(source.GetEnumerator(), sizes).Slice();
		}

		/// <summary>
		/// Slices the iteration over an async enumerable by the given slice sizes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The async source sequence to slice</param>
		/// <param name="sizes">
		/// Slice sizes. At least one size is required. Multiple sizes result in differently sized slices,
		/// whereat the last size is used for the "rest" (if any)
		/// </param>
		/// <returns>The sliced async enumerable</returns>
		public static IAsyncEnumerable<List<T>> SliceAsync<T>(this IAsyncEnumerable<T> source, params int[] sizes)
		{
			return SliceAsync(source, CancellationToken.None, sizes);
		}

		/// <summary>
		/// Slices the iteration over an async enumerable by the given slice sizes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The async source sequence to slice</param>
		/// <param name="sizes">
		/// Slice sizes. At least one size is required. Multiple sizes result in differently sized slices,
		/// whereat the last size is used for the "rest" (if any)
		/// </param>
		/// <returns>The sliced async enumerable</returns>
		public static IAsyncEnumerable<List<T>> SliceAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancelToken, params int[] sizes)
		{
            if (!sizes.Any(step => step != 0))
            {
                throw new InvalidOperationException("Can't slice a collection with step length 0.");
            }

            return new AsyncEnumerableSlicer<T>(source.GetAsyncEnumerator(cancelToken), sizes).SliceAsync(cancelToken);

            //return source.Batch(100);
        }
	}

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
            foreach (T t in source)
            {
                action(t);
            }
        }

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
				await action(t).ConfigureAwait(false);
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
				return DefaultReadOnlyCollection<T>.Empty;

			if (source is ReadOnlyCollection<T> readOnly)
			{
				return readOnly;
			}
			else if (source is List<T> list) 
			{
				return list.AsReadOnly();
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
				throw new ArgumentNullException(nameof(source));

			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));

			if (elementSelector == null)
				throw new ArgumentNullException(nameof(elementSelector));

			var dictionary = new Dictionary<TKey, TElement>(comparer);

			foreach (var local in source)
			{
				dictionary[keySelector(local)] = elementSelector(local);
			}

			return dictionary;
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
                throw new ArgumentNullException(nameof(source));

            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

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
		public static string[] ToStringArray(this IEnumerable<StringSegment> source)
		{
			return source.Select(x => x.ToString()).ToArray();
		}

		#endregion

		#region Async

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
				await action(t, i++).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Filters a sequence of values based on an async predicate.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source.</typeparam>
		/// <param name="source">A sequence to filter.</param>
		/// <param name="predicate">An async task function to test each element for a condition.</param>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
		public static async IAsyncEnumerable<T> WhereAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
		{
			await foreach (var item in source.ToAsyncEnumerable())
			{
				if (await predicate(item))
				{
					yield return item;
				}
			}
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
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
		{
			await foreach (var item in source.ToAsyncEnumerable())
			{
				yield return await selector(item);
			}
		}

		/// <summary>
		/// Awaits all tasks in a sequence to complete.
		/// </summary>
		public static async Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> source)
		{
			return await Task.WhenAll(source);
		}

		// TODO: (core) Probably conflicting with efcore AnyAsync extension method.
		/// <summary>
		/// Determines whether any element of a sequence satisfies a condition. 
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

		/// <summary>
		/// Creates a list of elements asynchronously from the enumerable source.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extensions methods.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source</typeparam>
		/// <param name="source">The collection of elements</param>
		/// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<List<T>> AsyncToList<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
			return source.ToListAsync(cancellationToken);
		}

		/// <summary>
		/// Creates an array of elements asynchronously from the enumerable source.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extensions methods.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source</typeparam>
		/// <param name="source">The collection of elements</param>
		/// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<T[]> AsyncToArray<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
		{
			return source.ToArrayAsync(cancellationToken);
		}

		#endregion
	}
}
