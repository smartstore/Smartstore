using System;
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
        /// <returns>First address matched by one property.</returns>
        public static Address FindAddress(this ICollection<Address> source, Address address)
        {
            return source.FindAddress(
                address.FirstName,
                address.LastName,
                address.PhoneNumber,
                address.Email,
                address.FaxNumber,
                address.Company,
                address.Address1,
                address.Address2,
                address.City,
                address.StateProvinceId,
                address.ZipPostalCode,
                address.CountryId
            );
        }

        /// <summary>
        /// Finds first occurrence of an address by single parameters.
        /// </summary>
        /// <param name="source">Addresses in which to search.</param>
        /// <returns>First address matched by one parameter.</returns>
        public static Address FindAddress(
            this ICollection<Address> source,
            string firstName,
            string lastName,
            string phoneNumber,
            string email,
            string faxNumber,
            string company,
            string address1,
            string address2,
            string city,
            int? stateProvinceId,
            string zipPostalCode,
            int? countryId)
        {
            Func<Address, bool> addressMatcher = (x) =>
            {
                return x.Email.EqualsNoCase(email)
                    && x.LastName.EqualsNoCase(lastName)
                    && x.FirstName.EqualsNoCase(firstName)
                    && x.Address1.EqualsNoCase(address1)
                    && x.Address2.EqualsNoCase(address2)
                    && x.Company.EqualsNoCase(company)
                    && x.ZipPostalCode.EqualsNoCase(zipPostalCode)
                    && x.City.EqualsNoCase(city)
                    && x.PhoneNumber.EqualsNoCase(phoneNumber)
                    && x.FaxNumber.EqualsNoCase(faxNumber)
                    && x.StateProvinceId == stateProvinceId
                    && x.CountryId == countryId;
            };

            return source.FirstOrDefault(addressMatcher);
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
                result = string.Format("{0} {1}", address.FirstName, address.LastName).Trim();
            }

            if (withCompanyName && address.Company.HasValue())
            {
                result = string.Concat(result, result.HasValue() ? ", " : "", address.Company);
            }

            return result;
        }

        /// <summary>
        /// Checks whether the postal data of two addresses are equal.
        /// </summary>
        public static bool IsPostalDataEqual(this Address address, Address other)
        {
            if (address != null && other != null)
            {
                if (address.FirstName.EqualsNoCase(other.FirstName) &&
                    address.LastName.EqualsNoCase(other.LastName) &&
                    address.Company.EqualsNoCase(other.Company) &&
                    address.Address1.EqualsNoCase(other.Address1) &&
                    address.Address2.EqualsNoCase(other.Address2) &&
                    address.ZipPostalCode.EqualsNoCase(other.ZipPostalCode) &&
                    address.City.EqualsNoCase(other.City) &&
                    address.StateProvinceId == other.StateProvinceId &&
                    address.CountryId == other.CountryId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
