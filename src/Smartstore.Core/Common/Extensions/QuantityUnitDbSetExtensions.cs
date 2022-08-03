using Smartstore.Core.Common;

namespace Smartstore
{
    public static partial class QuantityUnitDbSetExtensions
    {
        /// <summary>
        /// Gets a quantity unit by identifier. Loads the default quantity unit if none was found by <paramref name="quantityUnitId"/>.
        /// </summary>
        /// <param name="quantityUnits">Quantity units.</param>
        /// <param name="quantityUnitId">The quantity unit identifier.</param>
        /// <param name="fallbackToDefault">A value indicating whether to load the default quantity unit if none was found by <paramref name="quantityUnitId"/>.</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Found quantity unit or <c>null</c> if none was found.</returns>
        public static async Task<QuantityUnit> GetQuantityUnitByIdAsync(
            this DbSet<QuantityUnit> quantityUnits,
            int quantityUnitId,
            bool fallbackToDefault,
            bool tracked = false)
        {
            var quantityUnit = await quantityUnits.FindByIdAsync(quantityUnitId, tracked);

            if (quantityUnit == null && fallbackToDefault)
            {
                quantityUnit = await quantityUnits
                    .ApplyTracking(tracked)
                    .FirstOrDefaultAsync(x => x.IsDefault);
            }

            return quantityUnit;
        }
    }
}
