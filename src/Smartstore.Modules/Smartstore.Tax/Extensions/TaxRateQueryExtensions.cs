namespace Smartstore.Tax
{
    public static class TaxRateQueryExtensions
    {
        /// <summary>
        /// Applies a region filter and sorts by <see cref="TaxRateEntity.CountryId"/>, then by <see cref="TaxRateEntity.StateProvinceId"/>, 
        /// then by <see cref="TaxRateEntity.Zip"/>, then by <see cref="TaxRateEntity.TaxCategoryId"/>.
        /// </summary>
        /// <param name="query">TaxRateEntity query.</param>
        /// <param name="taxCategoryId">Tax category identifier.</param>
        /// <param name="countryId">Country identifier.</param>
        /// <param name="stateProvinceId">State province identifier.</param>
        /// <param name="zip">Zip code to filter by.</param>
        /// <returns>TaxRateEntity query.</returns>
        public static IQueryable<TaxRateEntity> ApplyRegionFilter(this IQueryable<TaxRateEntity> query,
            int? taxCategoryId,
            int? countryId,
            int? stateProvinceId,
            string zip)
        {
            Guard.NotNull(query, nameof(query));

            if (zip == null)
            {
                zip = string.Empty;
            }

            zip = zip.Trim();

            if (taxCategoryId > 0)
            {
                query = query.Where(x => x.TaxCategoryId == taxCategoryId);
            }

            if (countryId > 0)
            {
                query = query.Where(x => x.CountryId == countryId);
            }

            if (stateProvinceId > 0)
            {
                query = query.Where(x => x.StateProvinceId == stateProvinceId);
            }

            if (zip.HasValue())
            {
                query = query.Where(x => x.Zip == zip);
            }

            return query
                .OrderBy(x => x.CountryId)
                .ThenBy(x => x.StateProvinceId)
                .ThenBy(x => x.Zip)
                .ThenBy(x => x.TaxCategoryId);
        }
    }
}
