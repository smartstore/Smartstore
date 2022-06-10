using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Products
{
    internal class ProductReviewHelpfulnessMap : IEntityTypeConfiguration<ProductReviewHelpfulness>
    {
        public void Configure(EntityTypeBuilder<ProductReviewHelpfulness> builder)
        {
            builder.HasOne(c => c.ProductReview)
                .WithMany(c => c.ProductReviewHelpfulnessEntries)
                .HasForeignKey(c => c.ProductReviewId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents a product review helpfulness.
    /// </summary>
    [Table("ProductReviewHelpfulness")]
    public partial class ProductReviewHelpfulness : CustomerContent
    {
        public ProductReviewHelpfulness()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductReviewHelpfulness(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the product review identifier.
        /// </summary>
        public int ProductReviewId { get; set; }

        private ProductReview _productReview;
        /// <summary>
        /// Gets or sets the product review.
        /// </summary>
        public ProductReview ProductReview
        {
            get => _productReview ?? LazyLoader.Load(this, ref _productReview);
            set => _productReview = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a review is helpful.
        /// </summary>
        public bool WasHelpful { get; set; }
    }
}
