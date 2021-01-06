using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Customers;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Cart
{
    public class ShoppingCartItemMap : IEntityTypeConfiguration<ShoppingCartItem>
    {
        public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
        {
            builder.HasOne(x => x.Customer)
                .WithMany(x => x.ShoppingCartItems)
                .HasForeignKey(x => x.CustomerId)
                .IsRequired(false);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .IsRequired(false);

            builder.HasOne(x => x.BundleItem)
                .WithMany()
                .HasForeignKey(x => x.BundleItemId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a shopping cart item
    /// </summary>
    [Index(nameof(ShoppingCartTypeId), nameof(CustomerId), Name = "IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId")]
    public partial class ShoppingCartItem : EntityWithAttributes, IAuditable
    {
        private readonly ILazyLoader _lazyLoader;

        public ShoppingCartItem()
        {
        }

        public ShoppingCartItem(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
        /// Gets or sets the product variant attributes
        /// </summary>
        [MaxLength]
        public string AttributesXml { get; set; }

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
            get => _lazyLoader?.Load(this, ref _product) ?? _product;
            set => _product = value;
        }

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer
        {
            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
            set => _customer = value;
        }

        private ProductBundleItem _bundleItem;
        /// <summary>
        /// Gets or sets the product bundle item
        /// </summary>
        public ProductBundleItem BundleItem
        {
            get => _lazyLoader?.Load(this, ref _bundleItem) ?? _bundleItem;
            set => _bundleItem = value;
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is free shipping
        /// </summary>
        [NotMapped]
        public bool IsFreeShipping => Product is null || Product.IsFreeShipping;

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is ship enabled
        /// </summary>
        [NotMapped]
        public bool IsShippingEnabled => Product is not null && Product.IsShippingEnabled;

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is tax exempt
        /// </summary>
        [NotMapped]
        public bool IsTaxExempt => Product is not null && Product.IsTaxExempt;
    }
}