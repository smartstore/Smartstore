using System;
using System.Linq;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Localization
{
    public static partial class LanguageQueryExtensions
    {
        public static IOrderedQueryable<Language> ApplyStandardFilter(this IQueryable<Language> query, bool includeHidden = false, int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}