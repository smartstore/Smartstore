using Smartstore.Core.Common;
using System.Linq;

namespace Smartstore
{
    public static class CountryQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="Country.TwoLetterIsoCode"/> or <see cref="Country.ThreeLetterIsoCode"/>
        /// </summary>
        public static IQueryable<Country> ApplyIsoCodeFilter(this IQueryable<Country> query, string isoCode)
        {
            Guard.NotNull(query, nameof(query));

            if (isoCode.Length < 2 || isoCode.Length > 3)
            {
                return query;
            }

            query = from x in query
                    where x.TwoLetterIsoCode == isoCode || x.ThreeLetterIsoCode == isoCode
                    select x;

            return query;
        }
    }
}
