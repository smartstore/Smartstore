using Smartstore.Core.Common;

namespace Smartstore
{
    public static class AddressExtensions
    {
        /// <summary>
        /// Returns the full name of the address.
        /// </summary>
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

        // TODO: (mh) (core) > Evaluate & implement other relevant extension methods.
        // GetFullSalutaion can be ignored as it's only used in one occasion and doesn't really do anything.

    }
}
