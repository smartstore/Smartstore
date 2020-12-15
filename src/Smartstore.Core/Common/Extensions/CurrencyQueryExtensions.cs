using Smartstore.Core.Common;
using Smartstore.Core.Stores;
using System.Linq;

namespace Smartstore
{
    public static class CurrencyQueryExtensions
    {
        /// <summary>
        /// Applies store filter and sorts by <see cref="Currency.DisplayOrder"/>
        /// </summary>
        public static IOrderedQueryable<Currency> ApplyStandardFilter(this IQueryable<Currency> query, bool includeHidden = false, int storeId = 0)
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
