using Smartstore.Collections;

namespace Smartstore
{
    public static class PagedListExtensions
    {
        /// <summary>
        /// Returns a paged list from a source sequence.
        /// </summary>
        /// <param name="pageIndex">The 0-based page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged list</returns>
        public static IPagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            return new PagedListImpl<T>(source, pageIndex, pageSize);
        }

        /// <summary>
        /// Returns a paged list from a source sequence.
        /// </summary>
        /// <param name="pageIndex">The 0-based page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="totalCount">Total count</param>
        /// <returns>Paged list</returns>
        public static IPagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            return new PagedListImpl<T>(source, pageIndex, pageSize, totalCount);
        }

        /// <summary>
        /// Applies paging argument to given query.
        /// </summary>
        /// <param name="pageIndex">The 0-based page index</param>
        /// <param name="pageSize">Page size</param>
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, int pageIndex, int pageSize)
        {
            Guard.PagingArgsValid(pageIndex, pageSize, nameof(pageIndex), nameof(pageSize));

            if (pageIndex == 0 && pageSize == int.MaxValue)
            {
                // Paging unnecessary
                return source;
            }
            else
            {
                var skip = pageIndex * pageSize;
                return skip == 0
                    ? source.Take(pageSize)
                    : source.Skip(skip).Take(pageSize);
            }
        }
    }
}
