using System.Linq;
using Smartstore.Shipping.Domain;

namespace Smartstore.Shipping
{
    public static class ShippingRateByTotalQueryExtensions
    {
        /// <summary>
        /// Applies a region filter.
        /// </summary>
        /// <param name="query">ShippingByTotalEntity query.</param>
        /// <param name="countryId">Country identifier.</param>
        /// <param name="stateProvinceId">State province identifier.</param>
        /// <returns>ShippingByTotalEntity query.</returns>
        public static IQueryable<ShippingRateByTotal> ApplyRegionFilter(this IQueryable<ShippingRateByTotal> query, 
            int? countryId, 
            int? stateProvinceId)
        {
            Guard.NotNull(query, nameof(query));

            if (countryId > 0)
            {
                query = query.Where(x => x.CountryId.GetValueOrDefault() == countryId || x.CountryId.GetValueOrDefault() == 0);
            }

            if (stateProvinceId > 0)
            {
                query = query.Where(x => x.StateProvinceId.GetValueOrDefault() == stateProvinceId || x.StateProvinceId.GetValueOrDefault() == 0);
            }

            return query;
        }
    }
}
