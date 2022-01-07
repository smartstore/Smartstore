using Smartstore.Core.Common;
using Smartstore.Core.Stores;

namespace Smartstore
{
    public static class CurrencyQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Currency.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Currency query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Currency.Published"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Currency query.</returns>
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
