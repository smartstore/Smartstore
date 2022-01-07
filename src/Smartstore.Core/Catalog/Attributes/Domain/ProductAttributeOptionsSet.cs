using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductAttributeOptionsSetMap : IEntityTypeConfiguration<ProductAttributeOptionsSet>
    {
        public void Configure(EntityTypeBuilder<ProductAttributeOptionsSet> builder)
        {
            builder.HasOne(c => c.ProductAttribute)
                .WithMany(c => c.ProductAttributeOptionsSets)
                .HasForeignKey(c => c.ProductAttributeId);
        }
    }

    /// <summary>
    /// Represents an options set for a product attribute.
    /// </summary>
    public partial class ProductAttributeOptionsSet : EntityWithAttributes, INamedEntity
    {
        public ProductAttributeOptionsSet()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductAttributeOptionsSet(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the product attribute identifier.
        /// </summary>
        public int ProductAttributeId { get; set; }

        private ProductAttribute _productAttribute;
        /// <summary>
        /// Gets or sets the product attribute.
        /// </summary>
        public ProductAttribute ProductAttribute
        {
            get => _productAttribute ?? LazyLoader.Load(this, ref _productAttribute);
            set => _productAttribute = value;
        }

        private ICollection<ProductAttributeOption> _productAttributeOptions;
        /// <summary>
        /// Gets or sets the product attribute options.
        /// </summary>
        public ICollection<ProductAttributeOption> ProductAttributeOptions
        {
            get => _productAttributeOptions ?? LazyLoader.Load(this, ref _productAttributeOptions) ?? (_productAttributeOptions ??= new HashSet<ProductAttributeOption>());
            protected set => _productAttributeOptions = value;
        }
    }
}
