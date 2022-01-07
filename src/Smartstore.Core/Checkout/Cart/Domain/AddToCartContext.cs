using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a context of adding to the shopping cart
    /// </summary>
    public class AddToCartContext
    {
        private ProductVariantAttributeSelection _attributeSelection;
        private string _rawAttributes;
        /// <summary>
        /// Gets or sets warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets shopping cart item
        /// </summary>
        public ShoppingCartItem Item { get; set; }

        /// <summary>
        /// Gets or sets child shopping cart items
        /// </summary>
        public List<ShoppingCartItem> ChildItems { get; set; } = new();

        /// <summary>
        /// Gets or sets product bundle item
        /// </summary>
        public ProductBundleItem BundleItem { get; set; }

        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets the product
        /// </summary>
        public Product Product { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart type
        /// </summary>
        public ShoppingCartType CartType { get; set; }

        /// <summary>
        /// Gets or sets the product variant query
        /// </summary>
        public ProductVariantQuery VariantQuery { get; set; }

        /// <summary>
        /// Gets or sets the product variant attributes in XML or JSON format
        /// </summary>
        public string RawAttributes
        {
            get => _rawAttributes;
            set
            {
                _rawAttributes = value;
                _attributeSelection = null;
            }
        }

        /// <summary>
        /// Gets the product variant attribute selection.
        /// </summary>
        public ProductVariantAttributeSelection AttributeSelection
            => _attributeSelection ??= new(RawAttributes);

        /// <summary>
        /// Gets or sets the price entered by customer
        /// </summary>
        public Money CustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add required products
        /// </summary>
        public bool AutomaticallyAddRequiredProducts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add all bundle (child) products
        /// </summary>
        public bool AutomaticallyAddBundleProducts { get; set; }

        /// <summary>
        /// Gets or sets store identifier
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// Gets bundle item id
        /// </summary>
        public int BundleItemId
            => BundleItem is null ? 0 : BundleItem.Id;
    }
}
