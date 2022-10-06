using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductSpecificationAttributeMap : IEntityTypeConfiguration<ProductSpecificationAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductSpecificationAttribute> builder)
        {
            builder.HasOne(c => c.SpecificationAttributeOption)
                .WithMany(c => c.ProductSpecificationAttributes)
                .HasForeignKey(c => c.SpecificationAttributeOptionId);

            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductSpecificationAttributes)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product specification attribute mapping.
    /// </summary>
    [Table("Product_SpecificationAttribute_Mapping")]
    [Index(nameof(SpecificationAttributeOptionId), Name = "IX_SpecificationAttributeOptionId")]
    public partial class ProductSpecificationAttribute : BaseEntity, IDisplayOrder
    {
        public ProductSpecificationAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductSpecificationAttribute(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the specification attribute option identifier.
        /// </summary>
        public int SpecificationAttributeOptionId { get; set; }

        private SpecificationAttributeOption _specificationAttributeOption;
        /// <summary>
        /// Gets or sets the specification attribute option.
        /// </summary>
        public SpecificationAttributeOption SpecificationAttributeOption
        {
            get => _specificationAttributeOption ?? LazyLoader.Load(this, ref _specificationAttributeOption);
            set => _specificationAttributeOption = value;
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
        /// Gets or sets a value indicating whether the attribute can be filtered.
        /// Only effective in accordance with MegaSearchPlus module.
        /// </summary>
        public bool? AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute will be shown on the product page.
        /// </summary>
        public bool? ShowOnProductPage { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
