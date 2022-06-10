using Smartstore.Core.Common;
using Smartstore.Core.Stores;

namespace Smartstore
{
    public static partial class CountryQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Country.DisplayOrder"/>, then by <see cref="Country.Name"/>.
        /// </summary>
        /// <param name="query">Country query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Country.Published"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction. 0 to load all countries.</param>
        /// <returns>Country query.</returns>
        public static IOrderedQueryable<Country> ApplyStandardFilter(this IQueryable<Country> query, bool includeHidden = false, int storeId = 0)
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

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }

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
