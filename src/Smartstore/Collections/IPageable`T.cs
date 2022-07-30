namespace Smartstore.Collections
{
    /// <summary>
    /// A sequence of objects that has been split into pages.
    /// </summary>
    public interface IPageable<out T> : IPageable, IEnumerable<T>
    {
        /// <summary>
        /// Gets underlying query without any paging applied
        /// </summary>
        IQueryable<T> SourceQuery { get; }

        /// <summary>
        /// The total number of items asynchronously.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Applies the initial paging arguments to the source query
        /// </summary>
        /// <returns>A query with applied paging args</returns>
        IQueryable<T> ApplyPaging();
    }
}
