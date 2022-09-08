using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductAttributeOptionMap : IEntityTypeConfiguration<ProductAttributeOption>
    {
        public void Configure(EntityTypeBuilder<ProductAttributeOption> builder)
        {
            // INFO: DeleteBehavior.Cascade required otherwise System.InvalidOperationException when deleting ProductAttributeOptionsSet.
            builder.HasOne(c => c.ProductAttributeOptionsSet)
                .WithMany(c => c.ProductAttributeOptions)
                .HasForeignKey(c => c.ProductAttributeOptionsSetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product attribute option.
    /// </summary>
    public partial class ProductAttributeOption : BaseEntity, ILocalizedEntity, ISearchAlias, IDisplayOrder, ICloneable<ProductVariantAttributeValue>
    {
        public ProductAttributeOption()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductAttributeOption(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the product attribute options set identifier.
        /// </summary>
        public int ProductAttributeOptionsSetId { get; set; }

        private ProductAttributeOptionsSet _productAttributeOptionsSet;
        /// <summary>
        /// Gets or sets the product attribute options set.
        /// </summary>
        public ProductAttributeOptionsSet ProductAttributeOptionsSet
        {
            get => _productAttributeOptionsSet ?? LazyLoader.Load(this, ref _productAttributeOptionsSet);
            set => _productAttributeOptionsSet = value;
        }

        /// <summary>
        /// Gets or sets the option name.
        /// </summary>
        [StringLength(4000)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <inheritdoc/>
        [StringLength(100)]
        [LocalizedProperty]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int MediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the color RGB value (used with "Boxes" attribute type).
        /// </summary>
        [StringLength(100)]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the price adjustment.
        /// </summary>
        public decimal PriceAdjustment { get; set; }

        /// <summary>
        /// Gets or sets the weight adjustment.
        /// </summary>
        public decimal WeightAdjustment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the option is pre-selected.
        /// </summary>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the type identifier.
        /// </summary>
        public int ValueTypeId { get; set; }

        /// <summary>
        /// Gets or sets the product attribute value type.
        /// </summary>
        [NotMapped]
        public ProductVariantAttributeValueType ValueType
        {
            get => (ProductVariantAttributeValueType)ValueTypeId;
            set => ValueTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets the linked product identifier.
        /// </summary>
        public int LinkedProductId { get; set; }

        /// <summary>
        /// Gets or sets the quantity for the linked product.
        /// </summary>
        public int Quantity { get; set; }

        /// <inheritdoc/>
        public ProductVariantAttributeValue Clone()
        {
            var value = new ProductVariantAttributeValue
            {
                Alias = Alias,
                Name = Name,
                MediaFileId = MediaFileId,
                Color = Color,
                PriceAdjustment = PriceAdjustment,
                WeightAdjustment = WeightAdjustment,
                IsPreSelected = IsPreSelected,
                DisplayOrder = DisplayOrder,
                ValueTypeId = ValueTypeId,
                LinkedProductId = LinkedProductId,
                Quantity = Quantity
            };

            return value;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
