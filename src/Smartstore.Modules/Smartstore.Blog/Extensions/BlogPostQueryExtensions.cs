using System.Linq.Dynamic.Core;
using Autofac;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Stores;

namespace Smartstore
{
    public static partial class BlogPostQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts descending by <see cref="BlogPost.CreatedOnUtc"/>.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
        /// <param name="includeHidden">Applies filter by <see cref="BlogPost.Published"/>.</param>
        /// <returns>Ordered blog post query.</returns>
        public static IOrderedQueryable<BlogPost> ApplyStandardFilter(
            this IQueryable<BlogPost> query,
            int storeId,
            int languageId = 0,
            bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (languageId != 0)
            {
                query = query.Where(b => !b.LanguageId.HasValue || b.LanguageId == languageId);
            }

            if (!includeHidden)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(b => !b.StartDateUtc.HasValue || b.StartDateUtc <= utcNow);
                query = query.Where(b => !b.EndDateUtc.HasValue || b.EndDateUtc >= utcNow);
                query = query.Where(b => b.IsPublished);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies a time filter.
        /// </summary>
        /// <param name="query">BlogPost query.</param>
        /// <param name="dateFrom">Applies lower limit date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// <param name="dateTo">Applies upper limit date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// /// <param name="maxAge">Applies maximum age date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// <returns>BlogPost query.</returns>
        public static IQueryable<BlogPost> ApplyTimeFilter(
            this IQueryable<BlogPost> query,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            DateTime? maxAge = null)
        {
            Guard.NotNull(query, nameof(query));

            if (dateFrom.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc <= dateTo.Value);
            }

            if (maxAge.HasValue)
            {
                query = query.Where(b => b.CreatedOnUtc >= maxAge.Value);
            }

            return query;
        }
    }
}
