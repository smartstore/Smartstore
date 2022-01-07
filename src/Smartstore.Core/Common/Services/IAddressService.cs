using Smartstore.Core.Common.Settings;

namespace Smartstore.Core.Common.Services
{
    public partial interface IAddressService
    {
        /// <summary>
        /// Gets a value indicating whether address is valid (can be saved).
        /// </summary>
        /// <param name="address">Address to validate.</param>
        /// <returns>Result</returns>
        Task<bool> IsAddressValidAsync(Address address);

        /// <summary>
        /// Formats the address according to the countries address formatting template.
        /// </summary>
        /// <param name="settings">Address to format.</param>
        /// <param name="newLineToBr">Whether new lines should be replaced with html BR tags.</param>
        /// <returns>The formatted address.</returns>
        Task<string> FormatAddressAsync(CompanyInformationSettings settings, bool newLineToBr = false);

        /// <summary>
        /// Formats the address according to the countries address formatting template.
        /// </summary>
        /// <param name="address">Address to format.</param>
        /// <param name="newLineToBr">Whether new lines should be replaced with html BR tags.</param>
        /// <returns>The formatted address.</returns>
        Task<string> FormatAddressAsync(Address address, bool newLineToBr = false);

        /// <summary>
        /// Formats the address according to the countries address formatting template.
        /// </summary>
        /// <param name="address">Address to format. Usually passed by the template engine as a dictionary.</param>
        /// <param name="template">The (liquid) formatting template. If <c>null</c>, the system global template will be used.</param>
        /// <returns>The formatted address.</returns>
        Task<string> FormatAddressAsync(object address, string template = null, IFormatProvider formatProvider = null);
    }
}
