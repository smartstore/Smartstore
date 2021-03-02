using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Collections
{
    /// <summary>
    /// Paged list interface
    /// </summary>
    public interface IPagedList<T> : IList<T>, IPageable, IAsyncEnumerable<T>
    {
        /// <summary>
        /// Gets underlying query without any paging applied
        /// </summary>
        IQueryable<T> SourceQuery { get; }

        /// <summary>
        /// Allows modification of the underlying query before it is executed.
        /// </summary>
        /// <param name="modifier">The modifier function. The underlying query is passed, the modified query should be returned.</param>
        /// <returns>The current instance for chaining</returns>
        IPagedList<T> ModifyQuery(Func<IQueryable<T>, IQueryable<T>> modifier);

        /// <summary>
        /// Applies the initial paging arguments to the passed query
        /// </summary>
        /// <param name="query">The query</param>
        /// <returns>A query with applied paging args</returns>
        IQueryable<T> ApplyPaging(IQueryable<T> query);

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
        Task<IPagedList<T>> LoadAsync(bool force = false);

        /// <summary>
        /// The total number of items asynchronously.
        /// </summary>
        Task<int> GetTotalCountAsync();
    }
}
