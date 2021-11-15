using System;
using System.Linq;
using Smartstore.Shipping.Domain;
using Smartstore.Utilities;

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
        /// <param name="zip">Zip code to filter by.</param>
        /// <returns>ShippingByTotalEntity query.</returns>
        public static IQueryable<ShippingRateByTotal> ApplyRegionFilter(this IQueryable<ShippingRateByTotal> query, 
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
                // TODO: (mh) (core) Will throw: EF LINQ cannot translate this predicate.
                query = query.Where(x => (zip.IsEmpty() && x.Zip.IsEmpty()) || ZipMatches(zip, x.Zip));
            }

            return query;
        }

        private static bool ZipMatches(string zip, string pattern)
        {
            if (pattern.IsEmpty() || pattern == "*")
            {
                return true; // catch all
            }

            var patterns = pattern.Contains(',')
                ? pattern.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                : new string[] { pattern };

            try
            {
                foreach (var entry in patterns)
                {
                    var wildcard = new Wildcard(entry, true);
                    if (wildcard.IsMatch(zip))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return zip.EqualsNoCase(pattern);
            }

            return false;
        }
    }
}
