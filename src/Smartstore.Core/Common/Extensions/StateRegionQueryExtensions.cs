using Smartstore.Core.Common;

namespace Smartstore
{
    public static class StateRegionQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="StateProvince.CountryId"/> and orders by <see cref="StateProvince.DisplayOrder"/>
        /// </summary>
        /// <param name="countryId">Country identifier.</param>
        /// <param name="includeHidden">Applies filter by <see cref="StateProvince.Published"/>.</param>
        public static IOrderedQueryable<StateProvince> ApplyCountryFilter(this IQueryable<StateProvince> query, int countryId, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => x.CountryId == countryId);

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}
