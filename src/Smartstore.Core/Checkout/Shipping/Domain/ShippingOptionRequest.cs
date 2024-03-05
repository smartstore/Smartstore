using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Represents a request to get shipping options
    /// </summary>
    public partial class ShippingOptionRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether shipping methods must match cart rules.
        /// </summary>
        public bool MatchRules { get; init; } = true;

        /// <summary>
        /// Gets the context store identifier
        /// </summary>
        public int StoreId { get; init; }

        /// <summary>
        /// Gets the customer
        /// </summary>
        public Customer Customer { get; init; }

        /// <summary>
        /// Gets organized shopping cart items
        /// </summary>
        public List<OrganizedShoppingCartItem> Items { get; init; } = [];

        /// <summary>
        /// Gets the shipping address
        /// </summary>
        public Address ShippingAddress { get; init; }

        /// <summary>
        /// Gets or sets the sender's country
        /// </summary>
        public Country CountryFrom { get; set; }

        /// <summary>
        /// Gets the sender's state/province
        /// </summary>
        public StateProvince StateProvinceFrom { get; init; }

        /// <summary>
        /// Gets or sets the sender's zip/postal code
        /// </summary>
        public string ZipPostalCodeFrom { get; set; } = string.Empty;
    }
}