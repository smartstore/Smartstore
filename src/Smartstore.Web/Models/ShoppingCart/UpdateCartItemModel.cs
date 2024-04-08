using Newtonsoft.Json;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    public partial class UpdateCartItemModel : ModelBase
    {
        /// <summary>
        /// Identifier of <see cref="ShoppingCartItem"/>.
        /// </summary>
        [JsonProperty("cartItemId")]
        public int CartItemId { get; set; }

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
        /// A value indicating whether the cart item is active.
        /// </summary>
        [JsonProperty("active")]
        public bool? Active { get; set; }

        /// <summary>
        /// A value indicating whether to activate all cart items.
        /// </summary>
        [JsonProperty("activateAll")]
        public bool? ActivateAll { get; set; }

        /// <summary>
        /// The new cart item quantity.
        /// </summary>
        [JsonProperty("newQuantity")]
        public int? NewQuantity { get; set; }
    }
}
