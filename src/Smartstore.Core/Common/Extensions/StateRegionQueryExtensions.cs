using Smartstore.Core.Common;

namespace Smartstore
{
    public static class StateRegionQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="StateProvince.Abbreviation"/>
        /// </summary>
        public static IQueryable<StateProvince> ApplyAbbreviationFilter(this IQueryable<StateProvince> query, string abbreviation)
        {
            Guard.NotNull(query, nameof(query));

            query = from x in query
                    where x.Abbreviation == abbreviation
                    select x;

            return query;
        }

        /// <summary>
        /// Applies filter by <see cref="StateProvince.CountryId"/> and orders by <see cref="StateProvince.DisplayOrder"/>
        /// </summary>
        public static IQueryable<StateProvince> ApplyCountryFilter(this IQueryable<StateProvince> query, int countryId)
        {
            Guard.NotNull(query, nameof(query));

            // INFO: If countryId == 0 an empty query should be returned.
            //if (countryId == 0)
            //{
            //    return query;
            //}

            query = from x in query                   
                    where x.CountryId == countryId
                    orderby x.DisplayOrder
                    select x;

            return query;
        }
    }
}
