using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    public partial class UpdateCartItemModel : ModelBase
    {
        /// <summary>
        /// Identifier of <see cref="ShoppingCartItem"/>.
        /// </summary>
        public int CartItemId { get; set; }

        /// <summary>
        /// A value indicating whether the customer is on the cart page or on any other page.
        /// </summary>
        public bool IsCartPage { get; set; }

        /// <summary>
        /// A value indicating whether the <see cref="ShoppingCartType"/> is <see cref="ShoppingCartType.Wishlist"/> or <see cref="ShoppingCartType.ShoppingCart"/>.
        /// </summary>
        public bool IsWishlist { get; set; }

        /// <summary>
        /// A value indicating whether the cart item is active.
        /// </summary>
        public bool? Active { get; set; }

        /// <summary>
        /// A value indicating whether to activate all cart items.
        /// </summary>
        public bool? ActivateAll { get; set; }

        /// <summary>
        /// The new cart item quantity.
        /// </summary>
        public int? NewQuantity { get; set; }
    }
}
