//using System;
//using System.Linq;
//using Smartstore.Core.Content.News;
//using Smartstore.Core.Stores;

//namespace Smartstore
//{
//    public static partial class NewsItemQueryExtensions
//    {
//        /// <summary>
//        /// Applies standard filter and sorts descending by <see cref="NewsItem.CreatedOnUtc"/>.
//        /// </summary>
//        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
//        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
//        /// <param name="includeHidden">Applies filter by <see cref="NewsItem.Published"/>.</param>
//        /// <param name="maxAge">Applies maximum age date time filter by <see cref="NewsItem.CreatedOnUtc"/>.</param>
//        /// <param name="title">Applies filter by <see cref="NewsItem.Title"/>.</param>
//        /// <param name="intro">Applies filter by <see cref="NewsItem.Intro"/>.</param>
//        /// <param name="full">Applies filter by <see cref="NewsItem.Body"/>.</param>
//        /// <returns>Ordered news item query.</returns>
//        public static IOrderedQueryable<NewsItem> ApplyStandardFilter(
//            this IQueryable<NewsItem> query,
//            int storeId,
//            int languageId = 0,
//            bool includeHidden = false,
//            DateTime? maxAge = null,
//            string title = "",
//            string intro = "",
//            string full = "")
//        {
//            Guard.NotNull(query, nameof(query));

//            if (maxAge.HasValue)
//            {
//                query = query.Where(n => n.CreatedOnUtc >= maxAge.Value);
//            }

//            if (title.HasValue())
//            {
//                query = query.Where(n => n.Title.Contains(title));
//            }

//            if (intro.HasValue())
//            {
//                query = query.Where(n => n.Short.Contains(intro));
//            }

//            if (full.HasValue())
//            {
//                query = query.Where(n => n.Full.Contains(full));
//            }

//            if (languageId != 0)
//            {
//                query = query.Where(n => !n.LanguageId.HasValue || n.LanguageId == languageId);
//            }

//            if (!includeHidden)
//            {
//                var utcNow = DateTime.UtcNow;
//                query = query.Where(n => n.Published);
//                query = query.Where(n => !n.StartDateUtc.HasValue || n.StartDateUtc <= utcNow);
//                query = query.Where(n => !n.EndDateUtc.HasValue || n.EndDateUtc >= utcNow);
//            }

//            if (storeId > 0)
//            {
//                query = query.ApplyStoreFilter(storeId);
//            }

//            return query.OrderByDescending(x => x.CreatedOnUtc);
//        }
//    }
//}
