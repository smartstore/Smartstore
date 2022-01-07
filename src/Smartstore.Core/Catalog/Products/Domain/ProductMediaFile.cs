using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents a product media file mapping.
    /// </summary>
    [Table("Product_MediaFile_Mapping")]
    public partial class ProductMediaFile : BaseEntity, IMediaFile, IDisplayOrder
    {
        public ProductMediaFile()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductMediaFile(ILazyLoader lazyLoader)
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

        /// <inheritdoc/>
        public int MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <inheritdoc/>
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <inheritdoc/>
        public int DisplayOrder { get; set; }
    }
}
