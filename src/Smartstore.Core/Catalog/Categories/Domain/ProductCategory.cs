using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;

namespace Smartstore.Core.Catalog.Categories
{
    internal class ProductCategoryMap : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a product category mapping.
    /// </summary>
    [Table("Product_Category_Mapping")]
    [Index(nameof(IsFeaturedProduct), Name = "IX_IsFeaturedProduct")]
    [Index(nameof(IsSystemMapping), Name = "IX_IsSystemMapping")]
    [Index(nameof(CategoryId), Name = "IX_CategoryId")]
    [Index(nameof(CategoryId), nameof(ProductId), Name = "IX_PCM_Product_and_Category")]
    public partial class ProductCategory : BaseEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the category identifier.
        /// </summary>
        public int CategoryId { get; set; }

        private Category _category;
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public Category Category
        {
            get => _category ?? LazyLoader.Load(this, ref _category);
            set => _category = value;
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

        /// <summary>
        /// Indicates whether the mapping is created by the user or by the system.
        /// <c>False</c> by default (recommended).
        /// </summary>
        /// <remarks>
        /// System mappings are automatically added and deleted (!) by <see cref="ProductRuleEvaluatorTask"/>.
        /// </remarks>
        public bool IsSystemMapping { get; set; }
    }
}
