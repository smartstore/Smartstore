using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Products
{
    internal class ProductBundleItemMap : IEntityTypeConfiguration<ProductBundleItem>
    {
        public void Configure(EntityTypeBuilder<ProductBundleItem> builder)
        {
            // INFO: DeleteBehavior.ClientSetNull required because of cycles or multiple cascade paths.
            builder
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            // INFO: required because the property names do not meet the EF conventions.
            builder
                .HasOne(c => c.BundleProduct)
                .WithMany(c => c.ProductBundleItems)
                .HasForeignKey(c => c.BundleProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product bundle item.
    /// </summary>
    [LocalizedEntity("Published and Visible and Product.Published and !Product.Deleted")]
    public partial class ProductBundleItem : BaseEntity, IAuditable, ILocalizedEntity, IDisplayOrder, ICloneable<ProductBundleItem>
    {
        public ProductBundleItem()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductBundleItem(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public int ProductId { get; set; }

        private Product _product;
        /// <summary>
        /// Gets or sets the product.
        /// </summary>
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        /// <summary>
        /// Gets or sets the product identifier of the bundle product.
        /// </summary>
        public int BundleProductId { get; set; }

        private Product _bundleProduct;
        /// <summary>
        /// Gets or sets the bundle product.
        /// </summary>
        public Product BundleProduct
        {
            get => _bundleProduct ?? LazyLoader.Load(this, ref _bundleProduct);
            set => _bundleProduct = value;
        }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the discount value.
        /// </summary>
        public decimal? Discount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the discount amount is calculated by percentage.
        /// </summary>
        public bool DiscountPercentage { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to filter attributes.
        /// </summary>
        public bool FilterAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide the thumbnail.
        /// </summary>
        public bool HideThumbnail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is visible.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets a display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        private ICollection<ProductBundleItemAttributeFilter> _attributeFilters;
        /// <summary>
        /// Gets or sets the attribute filters.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<ProductBundleItemAttributeFilter> AttributeFilters
        {
            get => _attributeFilters ?? LazyLoader.Load(this, ref _attributeFilters) ?? (_attributeFilters ??= new HashSet<ProductBundleItemAttributeFilter>());
            protected set => _attributeFilters = value;
        }

        /// <inheritdoc/>
        public ProductBundleItem Clone()
        {
            var bundleItem = new ProductBundleItem
            {
                ProductId = ProductId,
                BundleProductId = BundleProductId,
                Quantity = Quantity,
                Discount = Discount,
                DiscountPercentage = DiscountPercentage,
                Name = Name,
                ShortDescription = ShortDescription,
                FilterAttributes = FilterAttributes,
                HideThumbnail = HideThumbnail,
                Visible = Visible,
                Published = Published,
                DisplayOrder = DisplayOrder,
                CreatedOnUtc = CreatedOnUtc,
                UpdatedOnUtc = UpdatedOnUtc
            };

            return bundleItem;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
