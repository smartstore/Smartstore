using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Media;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductMediaFileMap : IEntityTypeConfiguration<ProductMediaFile>
    {
        public void Configure(EntityTypeBuilder<ProductMediaFile> builder)
        {
            builder.HasQueryFilter(c => !c.Product.Deleted);

            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductPictures)
                .HasForeignKey(c => c.ProductId);
        }
    }

    /// <summary>
    /// Represents a product media file mapping.
    /// </summary>
    [Table("Product_MediaFile_Mapping")]
    public partial class ProductMediaFile : BaseEntity, IMediaFile
    {
        private readonly ILazyLoader _lazyLoader;

        public ProductMediaFile()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductMediaFile(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
            get => _lazyLoader?.Load(this, ref _product) ?? _product;
            set => _product = value;
        }

        /// <inheritdoc/>
        public int MediaFileId { get; set; }

        /// TODO: (mg) (core): Implement media file navigation property for ProductMediaFile.

        /// <inheritdoc/>
        public int DisplayOrder { get; set; }
    }
}
