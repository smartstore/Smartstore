using System.Text.Json.Serialization;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    /// <summary>
    /// Represents the origin of a cart item update operation and indicates where the update was initiated from.
    /// </summary>
    public enum UpdateCartItemOrigin
    {
        Cart,
        Wishlist,
        OffCanvasCart,
        OffCanvasWishlist
    }

    public partial class UpdateCartItemModel : ModelBase
    {
        /// <summary>
        /// Gets or sets the origin of the request.
        /// </summary>
        [JsonPropertyName("origin")]
        public UpdateCartItemOrigin Origin { get; set; }

        /// <summary>
        /// Identifier of <see cref="ShoppingCartItem"/>.
        /// </summary>
        [JsonPropertyName("cartItemId")]
        public int CartItemId { get; set; }

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
