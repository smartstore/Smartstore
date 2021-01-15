using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;

// Source: https://github.com/dotnet/reactive/tree/9329157592c13e97ce2d3251c91b2871aed875c9/Ix.NET/Source/System.Linq.Async/System/Linq/Operators

namespace Smartstore
{
    public static class AsyncEnumerableExtensions
    {
		#region To*

		/// <summary>
		/// Creates a list of elements asynchronously from the enumerable source.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extension methods.
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
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extension methods.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source</typeparam>
		/// <param name="source">The collection of elements</param>
		/// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<T[]> AsyncToArray<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
		{
			return source.ToArrayAsync(cancellationToken);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey, TValue}" /> from an <see cref="IAsyncEnumerable{T}" /> by enumerating it
		/// asynchronously according to a specified key selector function.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extension methods.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<Dictionary<TKey, TSource>> AsyncToDictionary<TKey, TSource>(this IAsyncEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			CancellationToken cancellationToken = default)
		{
			return source.ToDictionaryAsync(keySelector, cancellationToken);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey, TValue}" /> from an <see cref="IAsyncEnumerable{T}" /> by enumerating it
		/// asynchronously according to a specified key selector function.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extension methods.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<Dictionary<TKey, TElement>> AsyncToDictionary<TKey, TSource, TElement>(this IAsyncEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector,
			CancellationToken cancellationToken = default)
		{
			return source.ToDictionaryAsync(keySelector, elementSelector, cancellationToken);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey, TValue}" /> from an <see cref="IAsyncEnumerable{T}" /> by enumerating it
		/// asynchronously according to a specified key selector function.
		/// The strange naming is due to the fact that we want to avoid naming conflicts with EF extension methods.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
		/// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<Dictionary<TKey, TElement>> AsyncToDictionary<TKey, TSource, TElement>(this IAsyncEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector,
			IEqualityComparer<TKey> comparer,
			CancellationToken cancellationToken = default)
		{
			return source.ToDictionaryAsync(keySelector, elementSelector, comparer, cancellationToken);
		}

		#endregion

		#region Count

		/// <summary>
		/// Returns an async-enumerable sequence containing an <see cref="int" /> that represents the total number of elements in an async-enumerable sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence that contains elements to be counted.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with the number of elements in the input sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		/// <exception cref="OverflowException">(Asynchronous) The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));

			return source switch
			{
				ICollection<TSource> collection => new ValueTask<int>(collection.Count),
				ICollection collection => new ValueTask<int>(collection.Count),
				_ => Core(source, cancellationToken),
			};

			static async ValueTask<int> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
			{
				var count = 0;

				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					checked
					{
						count++;
					}
				}

				return count;
			}
		}

		/// <summary>
		/// Returns an async-enumerable sequence containing an <see cref="int" /> that represents how many elements in the specified async-enumerable sequence satisfy a condition.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence that contains elements to be counted.</param>
		/// <param name="predicate">A function to test each element for a condition.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with a number that represents how many elements in the input sequence satisfy the condition in the predicate function.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));
			Guard.NotNull(predicate, nameof(predicate));

			return Core(source, predicate, cancellationToken);

			static async ValueTask<int> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
			{
				var count = 0;

				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					if (predicate(item))
					{
						checked
						{
							count++;
						}
					}
				}

				return count;
			}
		}

		/// <summary>
		/// Returns an async-enumerable sequence containing an <see cref="long" /> that represents the total number of elements in an async-enumerable sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence that contains elements to be counted.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with the number of elements in the input sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		/// <exception cref="OverflowException">(Asynchronous) The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<long> LongCountAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));

			return source switch
			{
				ICollection<TSource> collection => new ValueTask<long>(collection.Count),
				ICollection collection => new ValueTask<long>(collection.Count),
				_ => Core(source, cancellationToken),
			};

			static async ValueTask<long> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
			{
				var count = 0L;

				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					checked
					{
						count++;
					}
				}

				return count;
			}
		}

		/// <summary>
		/// Returns an async-enumerable sequence containing an <see cref="long" /> that represents how many elements in the specified async-enumerable sequence satisfy a condition.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence that contains elements to be counted.</param>
		/// <param name="predicate">A function to test each element for a condition.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with a number that represents how many elements in the input sequence satisfy the condition in the predicate function.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<long> LongCountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));
			Guard.NotNull(predicate, nameof(predicate));

			return Core(source, predicate, cancellationToken);

			static async ValueTask<long> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
			{
				var count = 0L;

				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					if (predicate(item))
					{
						checked
						{
							count++;
						}
					}
				}

				return count;
			}
		}

		#endregion

		#region Sum

		/// <summary>
		/// Computes the sum of a sequence of <see cref="int" /> values.
		/// </summary>
		/// <param name="source">A sequence of <see cref="int" /> values to calculate the sum of.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with the sum of the values in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<int> SumAsync(this IAsyncEnumerable<int> source, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));

			return Core(source, cancellationToken);

			static async ValueTask<int> Core(IAsyncEnumerable<int> source, CancellationToken cancellationToken)
			{
				var sum = 0;

				await foreach (int value in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					checked
					{
						sum += value;
					}
				}

				return sum;
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="int" /> values that are obtained by invoking a transform function on each element of the input sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">A sequence of values that are used to calculate a sum.</param>
		/// <param name="selector">A transform function to apply to each element.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>An async-enumerable sequence containing a single element with the sum of the values in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<int> SumAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));
			Guard.NotNull(selector, nameof(selector));

			return Core(source, selector, cancellationToken);

			static async ValueTask<int> Core(IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken)
			{
				var sum = 0;

				await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					var value = selector(item);

					checked
					{
						sum += value;
					}
				}

				return sum;
			}
		}

		#endregion

		#region Min/Max

		/// <summary>
		/// Returns the minimum element in an async-enumerable sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence to determine the minimum element of.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>A ValueTask containing a single element with the minimum element in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<TSource> MinAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));

			if (default(TSource)! == null) // NB: Null value is desired; JIT-time check.
			{
				return Core(source, cancellationToken);

				static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TSource>.Default;

					TSource value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						do
						{
							if (!await e.MoveNextAsync())
							{
								return default!;
							}

							value = e.Current;
						}
						while (value == null);

						while (await e.MoveNextAsync())
						{
							var x = e.Current;

							if (x != null && comparer.Compare(x, value) < 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
			else
			{
				return Core(source, cancellationToken);

				static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TSource>.Default;

					TSource value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						if (!await e.MoveNextAsync())
						{
							throw Error.NoElements();
						}

						value = e.Current;

						while (await e.MoveNextAsync())
						{
							var x = e.Current;

							if (comparer.Compare(x, value) < 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and returns the minimum value.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <typeparam name="TResult">The type of the objects derived from the elements in the source sequence to determine the minimum of.</typeparam>
		/// <param name="source">An async-enumerable sequence to determine the minimum element of.</param>
		/// <param name="selector">A transform function to apply to each element.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>A ValueTask sequence containing a single element with the value that corresponds to the minimum element in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<TResult> MinAsync<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));
			Guard.NotNull(selector, nameof(selector));

			if (default(TResult)! == null) // NB: Null value is desired; JIT-time check.
			{
				return Core(source, selector, cancellationToken);

				static async ValueTask<TResult> Core(IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TResult>.Default;

					TResult value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						do
						{
							if (!await e.MoveNextAsync())
							{
								return default!;
							}

							value = selector(e.Current);
						}
						while (value == null);

						while (await e.MoveNextAsync())
						{
							var x = selector(e.Current);

							if (x != null && comparer.Compare(x, value) < 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
			else
			{
				return Core(source, selector, cancellationToken);

				static async ValueTask<TResult> Core(IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TResult>.Default;

					TResult value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						if (!await e.MoveNextAsync())
						{
							throw Error.NoElements();
						}

						value = selector(e.Current);

						while (await e.MoveNextAsync())
						{
							var x = selector(e.Current);

							if (comparer.Compare(x, value) < 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
		}

		/// <summary>
		/// Returns the maximum element in an async-enumerable sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <param name="source">An async-enumerable sequence to determine the maximum element of.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>A ValueTask containing a single element with the maximum element in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<TSource> MaxAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));

			if (default(TSource)! == null) // NB: Null value is desired; JIT-time check.
			{
				return Core(source, cancellationToken);

				static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TSource>.Default;

					TSource value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						do
						{
							if (!await e.MoveNextAsync())
							{
								return default!;
							}

							value = e.Current;
						}
						while (value == null);

						while (await e.MoveNextAsync())
						{
							var x = e.Current;

							if (x != null && comparer.Compare(x, value) > 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
			else
			{
				return Core(source, cancellationToken);

				static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TSource>.Default;

					TSource value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						if (!await e.MoveNextAsync())
						{
							throw Error.NoElements();
						}

						value = e.Current;

						while (await e.MoveNextAsync())
						{
							var x = e.Current;
							if (comparer.Compare(x, value) > 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and returns the maximum value.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
		/// <typeparam name="TResult">The type of the objects derived from the elements in the source sequence to determine the maximum of.</typeparam>
		/// <param name="source">An async-enumerable sequence to determine the minimum element of.</param>
		/// <param name="selector">A transform function to apply to each element.</param>
		/// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
		/// <returns>A ValueTask containing a single element with the value that corresponds to the maximum element in the source sequence.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null.</exception>
		/// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
		public static ValueTask<TResult> MaxAsync<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken = default)
		{
			Guard.NotNull(source, nameof(source));
			Guard.NotNull(selector, nameof(selector));

			if (default(TResult)! == null) // NB: Null value is desired; JIT-time check.
			{
				return Core(source, selector, cancellationToken);

				static async ValueTask<TResult> Core(IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TResult>.Default;

					TResult value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						do
						{
							if (!await e.MoveNextAsync())
							{
								return default!;
							}

							value = selector(e.Current);
						}
						while (value == null);

						while (await e.MoveNextAsync())
						{
							var x = selector(e.Current);

							if (x != null && comparer.Compare(x, value) > 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
			else
			{
				return Core(source, selector, cancellationToken);

				static async ValueTask<TResult> Core(IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken)
				{
					var comparer = Comparer<TResult>.Default;

					TResult value;

					await using (var e = source.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator())
					{
						if (!await e.MoveNextAsync())
						{
							throw Error.NoElements();
						}

						value = selector(e.Current);

						while (await e.MoveNextAsync())
						{
							var x = selector(e.Current);
							if (comparer.Compare(x, value) > 0)
							{
								value = x;
							}
						}
					}

					return value;
				}
			}
		}

		#endregion

		#region OfType/Cast

		// REVIEW: This is a non-standard LINQ operator, because we don't have a non-generic IAsyncEnumerable.
		//
		//         Unfortunately, this has limited use because it requires the source to be IAsyncEnumerable<object>,
		//         thus it doesn't bind for value types. Adding a first generic parameter for the element type of
		//         the source is not an option, because it would require users to specify two type arguments, unlike
		//         what's done in Enumerable.OfType. Should we move this method to Ix, thus doing away with OfType
		//         in the API surface altogether?

		/// <summary>
		/// Filters the elements of an async-enumerable sequence based on the specified type.
		/// </summary>
		/// <typeparam name="TResult">The type to filter the elements in the source sequence on.</typeparam>
		/// <param name="source">The async-enumerable sequence that contains the elements to be filtered.</param>
		/// <returns>An async-enumerable sequence that contains elements from the input sequence of type TResult.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
		{
			Guard.NotNull(source, nameof(source));

			return Core(source);

			static async IAsyncEnumerable<TResult> Core(IAsyncEnumerable<object> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await foreach (var obj in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					if (obj is TResult result)
					{
						yield return result;
					}
				}
			}
		}

		/// <summary>
		/// Converts the elements of an async-enumerable sequence to the specified type.
		/// </summary>
		/// <typeparam name="TResult">The type to convert the elements in the source sequence to.</typeparam>
		/// <param name="source">The async-enumerable sequence that contains the elements to be converted.</param>
		/// <returns>An async-enumerable sequence that contains each element of the source sequence converted to the specified type.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		public static IAsyncEnumerable<TResult> Cast<TResult>(this IAsyncEnumerable<object> source)
		{
			Guard.NotNull(source, nameof(source));

			if (source is IAsyncEnumerable<TResult> typedSource)
			{
				return typedSource;
			}

			return Core(source);

			static async IAsyncEnumerable<TResult> Core(IAsyncEnumerable<object> source, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await foreach (var obj in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					yield return (TResult)obj;
				}
			}
		}

		#endregion
	}
}
