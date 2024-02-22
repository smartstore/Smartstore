using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutAddressModel : ModelBase
    {
        public List<AddressModel> ExistingAddresses { get; set; } = [];
        public AddressModel NewAddress { get; set; } = new();

        public bool IsShippingRequired { get; set; }
        public bool ShippingAddressDiffers { get; set; }
    }
}
