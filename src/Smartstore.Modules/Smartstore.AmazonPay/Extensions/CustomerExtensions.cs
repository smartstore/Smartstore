using System.Linq;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.AmazonPay
{
    internal static class CustomerExtensions
    {
        public static Address FindAddress(this Customer customer, Address address)
        {
            Guard.NotNull(customer, nameof(customer));

            if (address == null)
            {
                return null;
            }

            var match = customer.Addresses.FindAddress(address);

            if (match == null)
            {
                // Also check incomplete "ToAddress".
                match = customer.Addresses.FirstOrDefault(x =>
                    x.FirstName == null && x.LastName == null &&
                    x.Address1 == null && x.Address2 == null &&
                    x.City == address.City && x.ZipPostalCode == address.ZipPostalCode &&
                    x.PhoneNumber == null &&
                    x.CountryId == address.CountryId && x.StateProvinceId == address.StateProvinceId
                );
            }

            return match;
        }
    }
}
