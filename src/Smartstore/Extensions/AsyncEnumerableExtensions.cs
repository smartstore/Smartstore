using System.Runtime.CompilerServices;

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
        public static ValueTask<List<T>> AsyncToList<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
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
        public static ValueTask<T[]> AsyncToArray<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
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
        public static ValueTask<Dictionary<TKey, TSource>> AsyncToDictionary<TKey, TSource>(this IAsyncEnumerable<TSource> source,
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
        public static ValueTask<Dictionary<TKey, TElement>> AsyncToDictionary<TKey, TSource, TElement>(this IAsyncEnumerable<TSource> source,
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
        public static ValueTask<Dictionary<TKey, TElement>> AsyncToDictionary<TKey, TSource, TElement>(this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
        {
            return source.ToDictionaryAsync(keySelector, elementSelector, comparer, cancellationToken);
        }

        #endregion

        #region Chunk

        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> whose elements to chunk.</param>
        /// <param name="size">Maximum size of each chunk.</param>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        public static IAsyncEnumerable<T[]> ChunkAsync<T>(this IAsyncEnumerable<T> source, int size, CancellationToken cancelToken = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.IsPositive(size, nameof(size));

            return AsyncChunkIterator(source, size, cancelToken);
        }

        private static async IAsyncEnumerable<TSource[]> AsyncChunkIterator<TSource>(IAsyncEnumerable<TSource> source, int size, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancelToken);
            while (await e.MoveNextAsync())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                int i = 1;
                for (; i < chunk.Length && await e.MoveNextAsync(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
        }

        #endregion
    }
}
