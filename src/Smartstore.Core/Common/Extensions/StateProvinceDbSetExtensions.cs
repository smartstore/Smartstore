using Smartstore.Core.Common;

namespace Smartstore
{
    public static partial class StateProvinceDbSetExtensions
    {
        /// <summary>
        /// Gets state provinces by country ID.
        /// </summary>
        /// <param name="stateProvinces">State provinces.</param>
        /// <param name="countryId">Country identifier.</param>
        /// <param name="includeHidden">Applies filter by <see cref="StateProvince.Published"/>.</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>State provinces.<c>null</c> if <paramref name="countryId"/> is 0.</returns>
        public static async Task<IList<StateProvince>> GetStateProvincesByCountryIdAsync(this DbSet<StateProvince> stateProvinces,
            int countryId,
            bool includeHidden = false,
            bool tracked = false)
        {
            Guard.NotNull(stateProvinces, nameof(stateProvinces));

            if (countryId > 0)
            {
                return await stateProvinces
                    .ApplyTracking(tracked)
                    .ApplyCountryFilter(countryId, includeHidden)
                    .ToListAsync();
            }

            return null;
        }
    }
}
