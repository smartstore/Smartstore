using System.Threading.Tasks;
using Smartstore.Core.Catalog;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Represents the quantity unit service.
    /// </summary>
    public partial interface IQuantityUnitService
    {
        /// <summary>
        /// Gets a quantity unit by identifier. Loads the default quantity unit if none was found by <paramref name="quantityUnitId"/>
        /// and <see cref="CatalogSettings.ShowDefaultQuantityUnit"/> is activated.
        /// </summary>
        /// <param name="quantityUnitId">The quantity unit identifier.</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Found quantity unit or <c>null</c> if none was found.</returns>
        Task<QuantityUnit> GetQuantityUnitByIdAsync(int? quantityUnitId, bool tracked = false);
    }
}
