using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Brands
{
    internal class ProductManufacturerMap : IEntityTypeConfiguration<ProductManufacturer>
    {
        public void Configure(EntityTypeBuilder<ProductManufacturer> builder)
        {
            builder.HasOne(c => c.Manufacturer)
                .WithMany()
                .HasForeignKey(c => c.ManufacturerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductManufacturers)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product manufacturer mapping.
    /// </summary>
    [Table("Product_Manufacturer_Mapping")]
    [Index(nameof(IsFeaturedProduct), Name = "IX_IsFeaturedProduct")]
    [Index(nameof(ManufacturerId), Name = "IX_ManufacturerId")]
    [Index(nameof(ManufacturerId), nameof(ProductId), Name = "IX_PMM_Product_and_Manufacturer")]
    public partial class ProductManufacturer : BaseEntity, IDisplayOrder
    {
        public ProductManufacturer()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductManufacturer(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the manufacturer identifier.
        /// </summary>
        public int ManufacturerId { get; set; }

        private Manufacturer _manufacturer;
        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        public Manufacturer Manufacturer
        {
            get => _manufacturer ?? LazyLoader.Load(this, ref _manufacturer);
            set => _manufacturer = value;
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
        /// Gets or sets a value indicating whether the product is featured.
        /// </summary>
        public bool IsFeaturedProduct { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
