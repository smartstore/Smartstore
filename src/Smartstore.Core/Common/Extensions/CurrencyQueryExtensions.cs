using Smartstore.Core.Common;
using System.Linq;

namespace Smartstore
{
    public static class CurrencyQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="Currency.CurrencyCode"/>. The caller is responsible for caching.
        /// </summary>
        public static IQueryable<Currency> ApplyCurrencyCodeFilter(this IQueryable<Currency> query, string currencyCode)
        {
            Guard.NotNull(query, nameof(query));

            query = from x in query
                    where x.CurrencyCode.ToLower() == currencyCode.ToLower()
                    select x;

            return query;
        }
    }
}
