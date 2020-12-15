using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Country service interface
    /// </summary>
    public partial interface ICountryService
    {
        /// <summary>
        /// Gets all countries and orders by <see cref="Country.DisplayOrder"/> then by <see cref="Country.Name"/>
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Country collection</returns>
        Task<IList<Country>> GetCountriesAsync(bool showHidden = false);

        /// <summary>
        /// Gets all countries that allow billing
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Country collection</returns>
        Task<IList<Country>> GetCountriesForBillingAsync(bool showHidden = false);

        /// <summary>
        /// Gets all countries that allow shipping
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Country collection</returns>
        Task<IList<Country>> GetCountriesForShippingAsync(bool showHidden = false);
    }
}