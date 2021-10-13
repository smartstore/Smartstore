using Smartstore.Admin.Models.Common;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Customers
{
    public class CustomerAddressModel : ModelBase
    {
        public int CustomerId { get; set; }
        public string Username { get; set; }

        [UIHint("Address")]
        public AddressModel Address { get; set; } = new();
    }
}
