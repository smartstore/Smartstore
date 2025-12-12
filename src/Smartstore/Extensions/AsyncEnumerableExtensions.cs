using System.Runtime.CompilerServices;

// Source: https://github.com/dotnet/reactive/tree/9329157592c13e97ce2d3251c91b2871aed875c9/Ix.NET/Source/System.Linq.Async/System/Linq/Operators

namespace Smartstore
{
    public static class AsyncEnumerableExtensions
    {
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
            Guard.NotNull(source);
            Guard.IsPositive(size);

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
