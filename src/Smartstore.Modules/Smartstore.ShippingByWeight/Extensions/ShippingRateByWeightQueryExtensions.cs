namespace Smartstore.ShippingByWeight
{
    public static class ShippingRateByWeightQueryExtensions
    {
        /// <summary>
        /// Applies a region filter.
        /// </summary>
        /// <param name="query">ShippingByWeightEntity query</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="zip">Zip code. Only excludes empty entities if param is empty itself. 
        /// Real filtering for specific zipcode must be done outside this filter.</param>
        /// <returns>ShippingByWeightEntity query</returns>
        public static IQueryable<ShippingRateByWeight> ApplyRegionFilter(this IQueryable<ShippingRateByWeight> query,
            int? countryId,
            string zip)
        {
            Guard.NotNull(query, nameof(query));

            if (countryId > 0)
            {
                query = query.Where(x => x.CountryId == countryId || x.CountryId == 0);
            }

            //var zipIsEmpty = zip.IsEmpty();
            //query = query.Where(x => (zipIsEmpty && string.IsNullOrEmpty(x.Zip)) || (!zipIsEmpty && !string.IsNullOrEmpty(x.Zip)));

            return query;
        }

        /// <summary>
        /// Applies a subtotal filter.
        /// </summary>
        /// <param name="query">ShippingByWeightEntity query</param>
        /// <param name="weight">Subtotal</param>
        /// <returns>ShippingByWeightEntity query</returns>
        public static IQueryable<ShippingRateByWeight> ApplyWeightFilter(this IQueryable<ShippingRateByWeight> query, decimal weight)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => weight >= x.From && weight <= x.To);

            return query;
        }
    }
}
