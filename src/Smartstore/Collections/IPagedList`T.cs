namespace Smartstore.Collections
{
    /// <summary>
    /// Paged list interface
    /// </summary>
    public interface IPagedList<T> : IList<T>, IPageable<T>
    {
        /// <summary>
        /// Returns this object typed as <see cref="IAsyncEnumerable{T}" />.
        /// </summary>
        /// <returns>This object.</returns>
        IAsyncEnumerable<T> AsAsyncEnumerable()
            => (IAsyncEnumerable<T>)this;

        /// <summary>
        /// Allows modification of the underlying query before it is executed.
        /// </summary>
        /// <param name="modifier">The modifier function. The underlying query is passed, the modified query should be returned.</param>
        /// <returns>The current instance for chaining</returns>
        IPagedList<T> ModifyQuery(Func<IQueryable<T>, IQueryable<T>> modifier);

        /// <summary>
        /// Loads the data synchronously.
        /// </summary>
        /// <param name="force">When <c>true</c>, always reloads data. When <c>false</c>, first checks to see whether data has been loaded already and skips if so.</param>
        /// <returns>Returns itself for chaining.</returns>
        IPagedList<T> Load(bool force = false);

        /// <summary>
        /// Loads the data asynchronously.
        /// </summary>
        /// <param name="force">When <c>true</c>, always reloads data. When <c>false</c>, first checks to see whether data has been loaded already and skips if so.</param>
        /// <returns>Returns itself for chaining.</returns>
        Task<IPagedList<T>> LoadAsync(bool force = false, CancellationToken cancelToken = default);
    }
}
