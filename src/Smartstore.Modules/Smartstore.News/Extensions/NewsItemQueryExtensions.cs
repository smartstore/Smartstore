using System.Linq.Dynamic.Core;
using Autofac;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Stores;

namespace Smartstore
{
    public static partial class NewsItemQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts descending by <see cref="NewsItem.CreatedOnUtc"/>.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
        /// <param name="includeHidden">Applies filter by <see cref="NewsItem.Published"/>.</param>
        /// <returns>Ordered news item query.</returns>
        public static IOrderedQueryable<NewsItem> ApplyStandardFilter(
            this IQueryable<NewsItem> query,
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
                query = query.Where(b => b.Published);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }
    }
}
