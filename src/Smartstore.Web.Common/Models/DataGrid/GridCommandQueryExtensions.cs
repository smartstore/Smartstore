using System.Linq.Dynamic.Core;
using Smartstore.Collections;

namespace Smartstore.Web.Models.DataGrid
{
    public static class GridCommandQueryExtensions
    {
        /// <summary>
        /// Returns a paged list from a source sequence by applying the paging settings in <paramref name="command"/>.
        /// </summary>
        /// <param name="GridCommand">The grid command to apply paging from.</param>
        /// <returns>Paged list</returns>
        public static IPagedList<T> ToPagedList<T>(this IEnumerable<T> source, GridCommand command)
        {
            return source.ToPagedList(command.Page - 1, command.PageSize);
        }

        /// <summary>
        /// Applies a bound <see cref="GridCommand"/> specification to a query.
        /// </summary>
        /// <param name="query">The query to apply the command to.</param>
        /// <param name="command">The source command.</param>
        /// <param name="applyPaging">Whether to apply the paging part also.</param>
        public static IQueryable<T> ApplyGridCommand<T>(this IQueryable<T> query, GridCommand command, bool applyPaging = false)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(command, nameof(command));

            IOrderedQueryable<T> orderedQuery = null;
            bool hasSorting = false;

            foreach (var sort in command.Sorting)
            {
                hasSorting = true;

                var expr = sort.Member;
                if (sort.Descending) expr += " descending";

                if (orderedQuery == null)
                {
                    orderedQuery = query.OrderBy(expr);
                }
                else
                {
                    orderedQuery = orderedQuery.ThenBy(expr);
                }

                query = orderedQuery;
            }

            applyPaging = applyPaging && command.PageSize < int.MaxValue;

            if (applyPaging && !hasSorting && query is IQueryable<BaseEntity> entityQuery)
            {
                query = entityQuery.OrderBy(x => x.Id).Cast<T>();
            }

            if (applyPaging)
            {
                query = query.ApplyPaging(command.Page - 1, command.PageSize);
            }

            return query;
        }
    }
}
