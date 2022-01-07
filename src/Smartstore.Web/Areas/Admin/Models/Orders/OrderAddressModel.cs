using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models.Orders
{
    public class OrderAddressModel : ModelBase
    {
        public int OrderId { get; set; }

        [UIHint("Address")]
        public AddressModel Address { get; set; } = new();
    }
}
