using System.Text.Json.Serialization;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    public partial class UpdateCartItemModel : ModelBase
    {
        /// <summary>
        /// Identifier of <see cref="ShoppingCartItem"/>.
        /// </summary>
        [JsonPropertyName("cartItemId")]
        public int CartItemId { get; set; }

        /// <summary>
        /// A value indicating whether the customer is on the cart page or on any other page.
        /// </summary>
        [JsonPropertyName("isCartPage")]
        public bool IsCartPage { get; set; }

        /// <summary>
        /// A value indicating whether the <see cref="ShoppingCartType"/> is <see cref="ShoppingCartType.Wishlist"/> or <see cref="ShoppingCartType.ShoppingCart"/>.
        /// </summary>
        [JsonPropertyName("isWishlist")]
        public bool IsWishlist { get; set; }

        /// <summary>
        /// A value indicating whether the cart item is active.
        /// </summary>
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        /// <summary>
        /// A value indicating whether to activate all cart items.
        /// </summary>
        [JsonPropertyName("activateAll")]
        public bool? ActivateAll { get; set; }

        /// <summary>
        /// The new cart item quantity.
        /// </summary>
        [JsonPropertyName("newQuantity")]
        public int? NewQuantity { get; set; }
    }
}
