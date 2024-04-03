using Newtonsoft.Json;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    public partial class UpdateCartItemModel : ModelBase
    {
        /// <summary>
        /// Identifier of <see cref="ShoppingCartItem"/>.
        /// </summary>
        [JsonProperty("sciItemId")]
        public int SciItemId { get; set; }

        /// <summary>
        /// A value indicating whether the customer is on the cart page or on any other page.
        /// </summary>
        [JsonProperty("isCartPage")]
        public bool IsCartPage { get; set; }

        /// <summary>
        /// A value indicating whether the <see cref="ShoppingCartType"/> is <see cref="ShoppingCartType.Wishlist"/> or <see cref="ShoppingCartType.ShoppingCart"/>.
        /// </summary>
        [JsonProperty("isWishlist")]
        public bool IsWishlist { get; set; }

        /// <summary>
        /// A value indicating whether the cart item is enabled.
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// A value indicating whether to enable/disable all cart items.
        /// </summary>
        [JsonProperty("enableAll")]
        public bool? EnableAll { get; set; }

        /// <summary>
        /// The new cart item quantity.
        /// </summary>
        [JsonProperty("newQuantity")]
        public int? NewQuantity { get; set; }
    }
}
