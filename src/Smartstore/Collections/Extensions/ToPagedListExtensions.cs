using System;
using System.Linq;
using Smartstore.Collections;

namespace Smartstore
{
    public static class ToPagedListExtensions
    {
        /// <summary>
        /// Returns a paged list from a source sequence.
        /// </summary>
        /// <param name="pageIndex">The 0-based page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged list</returns>
        public static PagedList<T> ToPagedList<T>(this IQueryable<T> source, int pageIndex, int pageSize)
        {
            return new PagedList<T>(source, pageIndex, pageSize);
        }

        /// <summary>
        /// Returns a paged list from a source sequence.
        /// </summary>
        /// <param name="pageIndex">The 0-based page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="totalCount">Total count</param>
        /// <returns>Paged list</returns>
        public static PagedList<T> ToPagedList<T>(this IQueryable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            return new PagedList<T>(source, pageIndex, pageSize, totalCount);
        }
    }
}
