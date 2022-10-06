using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore.Core.Checkout.Orders
{
    internal class OrderItemMap : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an order item
    /// </summary>
    public partial class OrderItem : BaseEntity, IAttributeAware
    {
        private ProductVariantAttributeSelection _attributeSelection;
        private string _rawAttributes;

        public OrderItem()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private OrderItem(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the order product variant identifier
        /// </summary>
        public Guid OrderItemGuid { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price in primary store currency (incl tax)
        /// </summary>
        public decimal UnitPriceInclTax { get; set; }

        /// <summary>
        /// Gets or sets the unit price in primary store currency (excl tax)
        /// </summary>
        public decimal UnitPriceExclTax { get; set; }

        /// <summary>
        /// Gets or sets the price in primary store currency (incl tax)
        /// </summary>
        public decimal PriceInclTax { get; set; }

        /// <summary>
        /// Gets or sets the price in primary store currency (excl tax)
        /// </summary>
        public decimal PriceExclTax { get; set; }

        /// <summary>
        /// Gets or sets the tax rate
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets or sets the discount amount (incl tax)
        /// </summary>
        public decimal DiscountAmountInclTax { get; set; }

        /// <summary>
        /// Gets or sets the discount amount (excl tax)
        /// </summary>
        public decimal DiscountAmountExclTax { get; set; }

        /// <summary>
        /// Gets or sets the attribute description
        /// </summary>
        public string AttributeDescription { get; set; }

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
        /// Gets or sets the download count
        /// </summary>
        public int DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether download is activated
        /// </summary>
        public bool IsDownloadActivated { get; set; }

        /// <summary>
        /// Gets or sets a license download identifier (in case this is a downloadable product)
        /// </summary>
        public int? LicenseDownloadId { get; set; }

        /// <summary>
        /// Gets or sets the total weight of one item
        /// It's nullable for compatibility with the previous version where was no such property
        /// </summary>
        public decimal? ItemWeight { get; set; }

        /// <summary>
        /// Gets or sets extra bundle data
        /// </summary>
        [MaxLength]
        public string BundleData { get; set; }

        /// <summary>
        /// Gets or sets the original product cost
        /// </summary>
        public decimal ProductCost { get; set; }

        /// <summary>
        /// Gets or sets the delivery time at the time of purchase.
        /// </summary>
        public int? DeliveryTimeId { get; set; }

        /// <summary>
        /// Indicates whether the delivery time was displayed at the time of purchase.
        /// </summary>
        public bool DisplayDeliveryTime { get; set; }

        private Order _order;
        /// <summary>
        /// Gets or sets the order
        /// </summary>
        public Order Order
        {
            get => _order ?? LazyLoader.Load(this, ref _order);
            set => _order = value;
        }

        private Product _product;
        /// <summary>
        /// Gets or sets the product
        /// </summary>
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        private ICollection<GiftCard> _associatedGiftCards;
        /// <summary>
        /// Gets or sets the associated gift card
        /// </summary>
        [IgnoreDataMember]
        public ICollection<GiftCard> AssociatedGiftCards
        {
            get => _associatedGiftCards ?? LazyLoader.Load(this, ref _associatedGiftCards) ?? (_associatedGiftCards ??= new HashSet<GiftCard>());
            protected set => _associatedGiftCards = value;
        }
    }
}