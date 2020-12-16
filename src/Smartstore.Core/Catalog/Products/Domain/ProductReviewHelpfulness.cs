using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductReviewHelpfulnessMap : IEntityTypeConfiguration<ProductReviewHelpfulness>
    {
        public void Configure(EntityTypeBuilder<ProductReviewHelpfulness> builder)
        {
            builder.HasOne(c => c.ProductReview)
                .WithMany(c => c.ProductReviewHelpfulnessEntries)
                .HasForeignKey(c => c.ProductReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product review helpfulness.
    /// </summary>
    public partial class ProductReviewHelpfulness : CustomerContent
    {
        private readonly ILazyLoader _lazyLoader;

        public ProductReviewHelpfulness()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductReviewHelpfulness(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
            get => _lazyLoader?.Load(this, ref _productReview) ?? _productReview;
            set => _productReview = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a review is helpful.
        /// </summary>
        public bool WasHelpful { get; set; }
    }
}
