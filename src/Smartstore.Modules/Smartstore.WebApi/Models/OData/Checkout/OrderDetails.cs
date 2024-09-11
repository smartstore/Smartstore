using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Models.Checkout
{
    /// <summary>
    /// Provides prepared order data.
    /// Returns data in context of language, currency conversion rate etc. that was applied at the time the order was placed.
    /// </summary>
    public partial class OrderDetails
    {
        /// <summary>
        /// Get the order identifier.
        /// </summary>
        public int Id { get; set; }

        // INFO: does not work. Entity and custom types cannot be mixed. Never serialized no matter what you set.
        //public Order Order { get; set; }

        /// <summary>
        /// Gets the order subtotal.
        /// </summary>
        public decimal OrderSubtotal { get; set; }

        /// <summary>
        /// Gets the order total.
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Gets a value indicating whether to display the product SKU according to <see cref="CatalogSettings.ShowProductSku"/>.
        /// </summary>
        public bool ShowSku { get; set; }

        /// <summary>
        /// Gets a value indicating whether to display the product images according to <see cref="ShoppingCartSettings.ShowProductImagesOnShoppingCart"/>.
        /// </summary>
        public bool ShowProductImages { get; set; }

        /// <summary>
        /// Gets a list of ordered items.
        /// </summary>
        public ICollection<OrderItemDetails> Items { get; set; }

        /// <summary>
        /// Provides prepared order item data like the SKU or image URL of the ordered product variant.
        /// </summary>
        public partial class OrderItemDetails
        {
            // INFO: OrderItemDetails will never be serialized if the property name is Id".
            /// <summary>
            /// Get the order item identifier.
            /// </summary>
            public int OrderItemId { get; set; }

            /// <summary>
            /// Get the product identifier.
            /// </summary>
            public int ProductId { get; set; }

            /// <summary>
            /// Gets a value indicating whether the product is soft-deleted.
            /// </summary>
            public bool IsProductSoftDeleted { get; set; }

            /// <summary>
            /// Gets the product variant SKU if a product variant was ordered, otherwise the product SKU.
            /// </summary>
            public string Sku { get; set; }

            /// <summary>
            /// Gets the product name in the language chosen by the buyer at the time the order was placed.
            /// </summary>
            public string ProductName { get; set; }

            /// <summary>
            /// Gets the slug of the product URL.
            /// </summary>
            /// <example>my-red-iphone</example>
            public string ProductSlug { get; set; }

            /// <summary>
            /// Gets the product URL.
            /// </summary>
            /// <example>/my-red-iphone?pvari4011-0-10-811=3360</example>
            public string ProductUrl { get; set; }

            /// <summary>
            /// Gets the unit price converted to the rate that was applied at the time the order was placed.
            /// </summary>
            /// <example>37.23</example>
            public decimal UnitPrice { get; set; }

            /// <summary>
            /// Gets the subtotal converted to the rate that was applied at the time the order was placed.
            /// </summary>
            /// <example>186.15</example>
            public decimal SubTotal { get; set; }

            /// <summary>
            /// The URL of the product image.
            /// The current host name is prepended if <see cref="MediaSettings.AutoGenerateAbsoluteUrls"/> is enabled.
            /// </summary>
            /// <example>//www.my-shop.com/media/40/catalog/my-red-iphone.jpg</example>
            public string ProductImageUrl { get; set; }

            /// <summary>
            /// The thumbnail URL of the product image.
            /// The current host name is prepended if <see cref="MediaSettings.AutoGenerateAbsoluteUrls"/> is enabled.
            /// </summary>
            /// <example>//www.my-shop.com/media/40/catalog/my-red-iphone.jpg?size=256</example>
            public string ProductThumbUrl { get; set; }
        }
    }
}
