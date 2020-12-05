using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Common;

namespace Smartstore.Core.Customers
{
    public class CustomerAttributeCollection : GenericAttributeCollection
    {
        public CustomerAttributeCollection(GenericAttributeCollection collection)
            : base(collection)
        {
        }

        #region Form fields

        public string StreetAddress
        {
            get => Get<string>(SystemCustomerAttributeNames.StreetAddress);
            set => Set(SystemCustomerAttributeNames.StreetAddress, value);
        }

        public int? CountryId
        {
            get => Get<int?>(SystemCustomerAttributeNames.CountryId);
            set => Set(SystemCustomerAttributeNames.CountryId, value);
        }

        #endregion

        #region Other attributes

        #endregion

        #region Depends on store

        public int? CurrencyId
        {
            get => Get<int?>(SystemCustomerAttributeNames.CurrencyId);
            set => Set(SystemCustomerAttributeNames.CurrencyId, value);
        }

        public int? LanguageId
        {
            get => Get<int?>(SystemCustomerAttributeNames.LanguageId);
            set => Set(SystemCustomerAttributeNames.LanguageId, value);
        }

        #endregion
    }
}
