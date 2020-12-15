using Smartstore.Core.Common;
using System.Linq;

namespace Smartstore
{
    public static class CurrencyQueryExtensions
    {
        //public static IOrderedQueryable<Currency> ApplyStandardFilter(this IQueryable<Currency> query, bool includeHidden = false, int storeId = 0)
        //{
        //    Guard.NotNull(query, nameof(query));

        //    if (!includeHidden)
        //    {
        //        query = query.Where(x => x.Published);
        //    }

        //    if (storeId > 0)
        //    {
        //        query = query.ApplyStoreFilter(storeId);
        //    }

        //    return query.OrderBy(x => x.DisplayOrder);
        //}
    }
}
