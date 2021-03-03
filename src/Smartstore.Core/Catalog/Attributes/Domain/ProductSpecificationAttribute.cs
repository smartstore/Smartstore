using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Domain;

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
                .IsRequired(false);

            builder
                .HasIndex(x => x.AllowFiltering, "IX_PSAM_AllowFiltering")
                .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId), nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId));

            builder
                .HasIndex(new[] { nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId), nameof(ProductSpecificationAttribute.AllowFiltering) }, "IX_PSAM_SpecificationAttributeOptionId_AllowFiltering")
                .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId));
        }
    }

    /// <summary>
    /// Represents a product specification attribute mapping.
    /// </summary>
    [Table("Product_SpecificationAttribute_Mapping")]
    public partial class ProductSpecificationAttribute : BaseEntity, IDisplayOrder
    {
        private readonly ILazyLoader _lazyLoader;

        public ProductSpecificationAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductSpecificationAttribute(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
            get => _lazyLoader?.Load(this, ref _specificationAttributeOption) ?? _specificationAttributeOption;
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
        [JsonIgnore]
        public Product Product
        {
            get => _lazyLoader?.Load(this, ref _product) ?? _product;
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
