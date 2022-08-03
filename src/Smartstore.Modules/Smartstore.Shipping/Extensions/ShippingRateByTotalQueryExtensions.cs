namespace Smartstore.Shipping
{
    public static class ShippingRateByTotalQueryExtensions
    {
        /// <summary>
        /// Applies a region filter.
        /// </summary>
        /// <param name="query">ShippingByTotalEntity query</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State province identifier</param>
        /// <param name="zip">Zip code. Only excludes empty entities if param is empty itself. 
        /// Real filtering for specific zipcode must be done outside this filter.</param>
        /// <returns>ShippingByTotalEntity query</returns>
        public static IQueryable<ShippingRateByTotal> ApplyRegionFilter(this IQueryable<ShippingRateByTotal> query,
            int? countryId,
            int? stateProvinceId)
        {
            Guard.NotNull(query, nameof(query));

            if (countryId > 0)
            {
                query = query.Where(x => x.CountryId == countryId || x.CountryId == 0 || x.CountryId == null);
            }

            if (stateProvinceId > 0)
            {
                query = query.Where(x => x.StateProvinceId == stateProvinceId || x.StateProvinceId == 0 || x.StateProvinceId == null);
            }

            return query;
        }

        /// <summary>
        /// Applies a subtotal filter.
        /// </summary>
        /// <param name="query">ShippingByTotalEntity query</param>
        /// <param name="subtotal">Subtotal</param>
        /// <returns>ShippingByTotalEntity query</returns>
        public static IQueryable<ShippingRateByTotal> ApplySubTotalFilter(this IQueryable<ShippingRateByTotal> query, decimal subtotal)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => subtotal >= x.From && (x.To == null || subtotal <= x.To.Value));

            return query;
        }
    }
}
