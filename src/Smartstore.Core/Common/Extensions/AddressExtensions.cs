using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Common;

namespace Smartstore
{
    public static class AddressExtensions
    {
        /// <summary>
        /// Finds first occurrence of an address.
        /// </summary>
        /// <param name="source">Addresses in which to search.</param>
        /// <param name="address">Address to find.</param>
        /// <param name="email">Specifies whether addresses must match per email. If not empty the parameter must match the email of the returned address.</param>
        /// <returns>First matched address.</returns>
        public static Address FindAddress(this ICollection<Address> source, Address address, string email = "")
        {
            var found = source.FirstOrDefault(x => x == address);

            if (found != null && email.HasValue() && email != found.Email)
            {
                return null;
            }

            return found;
        }

        /// <summary>
        /// Finds first occurrence of an address and patches it by adding <see cref="Address.PhoneNumber"/> and <see cref="Address.FaxNumber"/> if target properties are empty.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <param name="source">Addresses in which to search.</param>
        /// <param name="address">Address to find.</param>
        /// <param name="email">Specifies whether addresses must match per email. If not empty the parameter must match the email of the returned address.</param>
        /// <returns>First matched address, patched with <see cref="Address.PhoneNumber"/> and <see cref="Address.FaxNumber"/>.</returns>
        public static Address PatchAddress(this ICollection<Address> source, Address address, string email = "")
        {
            var found = source.FindAddress(address, email);

            if (found != null)
            {
                if (!found.PhoneNumber.HasValue())
                {
                    found.PhoneNumber = address.PhoneNumber;
                }

                if (!found.FaxNumber.HasValue())
                {
                    found.FaxNumber = address.FaxNumber;
                }
            }
            
            return found;
        }

        /// <summary>
        /// Returns the full name of the address.
        /// </summary>
        /// <param name="withCompanyName">Specifies wheter to include the company name.</param>
        /// <returns>"FirstName LastName, Company"</returns>
        public static string GetFullName(this Address address, bool withCompanyName = true)
        {
            if (address == null)
                return null;

            string result = string.Empty;
            if (address.FirstName.HasValue() || address.LastName.HasValue())
            {
                result = (address.FirstName + ' ' + address.LastName).Trim();
            }

            if (withCompanyName && address.Company.HasValue())
            {
                result = string.Concat(result, result.HasValue() ? ", " : string.Empty, address.Company);
            }

            return result;
        }
    }
}
