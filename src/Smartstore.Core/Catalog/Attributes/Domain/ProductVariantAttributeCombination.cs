using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductVariantAttributeCombinationMap : IEntityTypeConfiguration<ProductVariantAttributeCombination>
    {
        public void Configure(EntityTypeBuilder<ProductVariantAttributeCombination> builder)
        {
            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductVariantAttributeCombinations)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.DeliveryTime)
                .WithMany()
                .HasForeignKey(c => c.DeliveryTimeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.QuantityUnit)
                .WithMany()
                .HasForeignKey(c => c.QuantityUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a product variant attribute combination.
    /// </summary>
    [Index(nameof(Sku), Name = "IX_ProductVariantAttributeCombination_SKU")]
    [Index(nameof(Gtin), Name = "IX_Gtin")]
    [Index(nameof(ManufacturerPartNumber), Name = "IX_ManufacturerPartNumber")]
    [Index(nameof(IsActive), Name = "IX_IsActive")]
    [Index(nameof(StockQuantity), nameof(AllowOutOfStockOrders), Name = "IX_StockQuantity_AllowOutOfStockOrders")]
    public partial class ProductVariantAttributeCombination : BaseEntity, IAttributeAware
    {
        private ProductVariantAttributeSelection _attributeSelection;
        private string _rawAttributes;

        public ProductVariantAttributeCombination()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductVariantAttributeCombination(ILazyLoader lazyLoader)
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
        [IgnoreDataMember]
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        /// <summary>
        /// Gets or sets the SKU.
        /// </summary>
        [StringLength(400)]
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the GTIN.
        /// </summary>
        [StringLength(400)]
        public string Gtin { get; set; }

        /// <summary>
        /// Gets or sets the MPN.
        /// </summary>
        [StringLength(400)]
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public decimal? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// Gets or sets the amount of product per packing unit in the given measure unit 
        /// (e.g. 250 ml shower gel: "0.25" if MeasureUnit = "liter" and BaseAmount = 1).
        /// </summary>
        public decimal? BasePriceAmount { get; set; }

        /// <summary>
        /// Gets or sets the reference value for the given measure unit 
        /// (e.g. "1" liter. Formula: [BaseAmount] [MeasureUnit] = [SellingPrice] / [Amount]).
        /// </summary>
        public int? BasePriceBaseAmount { get; set; }

        /// <summary>
        /// Gets or sets the assigned media file identifiers.
        /// </summary>
        [StringLength(1000)]
        public string AssignedMediaFileIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute combination is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the delivery time identifier.
        /// </summary>
        public int? DeliveryTimeId { get; set; }

        private DeliveryTime _deliveryTime;
        /// <summary>
        /// Gets or sets the delivery time.
        /// </summary>
        public DeliveryTime DeliveryTime
        {
            get => _deliveryTime ?? LazyLoader.Load(this, ref _deliveryTime);
            set => _deliveryTime = value;
        }

        /// <summary>
        /// Gets or sets the quantity unit identifier.
        /// </summary>
        public int? QuantityUnitId { get; set; }

        private QuantityUnit _quantityUnit;
        /// <summary>
        /// Gets or sets the quantity unit.
        /// </summary>
        public QuantityUnit QuantityUnit
        {
            get => LazyLoader.Load(this, ref _quantityUnit) ?? _quantityUnit;
            set => _quantityUnit = value;
        }

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
        /// Gets or sets the stock quantity.
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow orders when out of stock.
        /// </summary>
        public bool AllowOutOfStockOrders { get; set; }

        /// <summary>
        /// Gets the assigned media file identifiers.
        /// </summary>
        public int[] GetAssignedMediaIds()
        {
            if (string.IsNullOrEmpty(AssignedMediaFileIds))
            {
                return Array.Empty<int>();
            }

            var query =
                from id in AssignedMediaFileIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                let idx = id.ToInt()
                where idx > 0
                select idx;

            return query.Distinct().ToArray();
        }

        /// <summary>
        /// Sets the assigned media file identifiers.
        /// </summary>
        public void SetAssignedMediaIds(int[] ids)
        {
            AssignedMediaFileIds = ids?.Length > 0
                ? string.Join(",", ids)
                : null;
        }
    }
}
