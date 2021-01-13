using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductReviewMap : IEntityTypeConfiguration<ProductReview>
    {
        public void Configure(EntityTypeBuilder<ProductReview> builder)
        {
            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductReviews)
                .HasForeignKey(c => c.ProductId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents a product review.
    /// </summary>
    [Table("ProductReview")] // Enables EF TPT inheritance
    public partial class ProductReview : CustomerContent
    {
        private readonly ILazyLoader _lazyLoader;

        public ProductReview()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductReview(ILazyLoader lazyLoader)
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

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [StringLength(4000)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the review text.
        /// </summary>
        [MaxLength]
        public string ReviewText { get; set; }

        /// <summary>
        /// Gets or sets the review rating.
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Gets or sets the review helpful votes total.
        /// </summary>
        public int HelpfulYesTotal { get; set; }

        /// <summary>
        /// Gets or sets the review not helpful votes total.
        /// </summary>
        public int HelpfulNoTotal { get; set; }

        private ICollection<ProductReviewHelpfulness> _productReviewHelpfulnessEntries;
        /// <summary>
        /// Gets or sets the entries of product review helpfulness.
        /// </summary>
        public ICollection<ProductReviewHelpfulness> ProductReviewHelpfulnessEntries
        {
            get => _lazyLoader?.Load(this, ref _productReviewHelpfulnessEntries) ?? (_productReviewHelpfulnessEntries ??= new HashSet<ProductReviewHelpfulness>());
            protected set => _productReviewHelpfulnessEntries = value;
        }
    }
}
