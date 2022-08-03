using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a shopping cart item
    /// </summary>
    [Index(nameof(ShoppingCartTypeId), nameof(CustomerId), Name = "IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId")]
    public partial class ShoppingCartItem : EntityWithAttributes, IAuditable, IAttributeAware
    {
        private ProductVariantAttributeSelection _attributeSelection;
        private string _rawAttributes;

        public ShoppingCartItem()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ShoppingCartItem(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// The parent shopping cart item identifier
        /// </summary>
        public int? ParentItemId { get; set; }

        /// <summary>
        /// Gets or sets ths bundle item identifier
        /// </summary>
        public int? BundleItemId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
		public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the product variant attributes in XML or JSON format
        /// </summary>
        [Column("AttributesXml"), MaxLength]
        public string RawAttributes
        {
            get => _rawAttributes;
            set
            {
                _rawAttributes = value;
                _attributeSelection = null;
            }
        }

        [NotMapped]
        public ProductVariantAttributeSelection AttributeSelection
            => _attributeSelection ??= new(RawAttributes);

        /// <summary>
        /// Gets or sets the price enter by a customer
        /// </summary>
        public decimal CustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart type identifier
        /// </summary>
        public int ShoppingCartTypeId { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart type
        /// </summary>
        [NotMapped]
        public ShoppingCartType ShoppingCartType
        {
            get => (ShoppingCartType)ShoppingCartTypeId;
            set => ShoppingCartTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        private Product _product;
        /// <summary>
        /// Gets or sets the product
        /// </summary>        
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }

        private ProductBundleItem _bundleItem;
        /// <summary>
        /// Gets or sets the product bundle item
        /// </summary>
        public ProductBundleItem BundleItem
        {
            get => _bundleItem ?? LazyLoader.Load(this, ref _bundleItem);
            set => _bundleItem = value;
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is free shipping
        /// </summary>
        [NotMapped]
        public bool IsFreeShipping
            => Product is null || Product.IsFreeShipping;

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is ship enabled
        /// </summary>
        [NotMapped]
        public bool IsShippingEnabled
            => Product != null && Product.IsShippingEnabled;

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is tax exempt
        /// </summary>
        [NotMapped]
        public bool IsTaxExempt
            => Product != null && Product.IsTaxExempt;

    }
}