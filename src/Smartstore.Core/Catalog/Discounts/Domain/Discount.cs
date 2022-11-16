using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Catalog.Discounts
{
    internal class DiscountMap : IEntityTypeConfiguration<Discount>
    {
        public void Configure(EntityTypeBuilder<Discount> builder)
        {
            builder
                .HasMany(c => c.RuleSets)
                .WithMany(c => c.Discounts)
                .UsingEntity<Dictionary<string, object>>(
                    "RuleSet_Discount_Mapping",
                    c => c
                        .HasOne<RuleSetEntity>()
                        .WithMany()
                        .HasForeignKey("RuleSetEntity_Id")
                        .HasConstraintName("FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Discount>()
                        .WithMany()
                        .HasForeignKey("Discount_Id")
                        .HasConstraintName("FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Discount_Id");
                        c.HasKey("Discount_Id", "RuleSetEntity_Id");
                    });

            builder
                .HasMany(c => c.AppliedToManufacturers)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToManufacturers",
                    c => c
                        .HasOne<Manufacturer>()
                        .WithMany()
                        .HasForeignKey("Manufacturer_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufacturer_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Discount>()
                        .WithMany()
                        .HasForeignKey("Discount_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Discount_Id");
                        c.HasKey("Discount_Id", "Manufacturer_Id");
                    });

            builder
                .HasMany(c => c.AppliedToCategories)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToCategories",
                    c => c
                        .HasOne<Category>()
                        .WithMany()
                        .HasForeignKey("Category_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Discount>()
                        .WithMany()
                        .HasForeignKey("Discount_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Discount_Id");
                        c.HasKey("Discount_Id", "Category_Id");
                    });

            builder
                .HasMany(c => c.AppliedToProducts)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToProducts",
                    c => c
                        .HasOne<Product>()
                        .WithMany()
                        .HasForeignKey("Product_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Discount>()
                        .WithMany()
                        .HasForeignKey("Discount_Id")
                        .HasConstraintName("FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Discount_Id");
                        c.HasKey("Discount_Id", "Product_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a discount.
    /// </summary>
    [DebuggerDisplay("{Name} - {DiscountType}")]
    [CacheableEntity]
    public partial class Discount : EntityWithAttributes, ILocalizedEntity, IRulesContainer
    {
        public Discount()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Discount(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the discount type identifier.
        /// </summary>
        public int DiscountTypeId { get; set; }

        /// <summary>
        /// Gets or sets the discount type.
        /// </summary>
		[NotMapped]
        public DiscountType DiscountType
        {
            get => (DiscountType)DiscountTypeId;
            set => DiscountTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the discount amount is calculated by percentage.
        /// </summary>
        public bool UsePercentage { get; set; }

        /// <summary>
        /// Gets or sets the discount percentage.
        /// </summary>
        public decimal DiscountPercentage { get; set; }

        /// <summary>
        /// Gets or sets the discount amount.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Gets or sets the discount start date.
        /// </summary>
        public DateTime? StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the discount end date.
        /// </summary>
        public DateTime? EndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the discount requires a coupon code.
        /// </summary>
        public bool RequiresCouponCode { get; set; }

        /// <summary>
        /// Gets or sets the coupon code.
        /// </summary>
        [StringLength(100)]
        public string CouponCode { get; set; }

        /// <summary>
        /// Gets or sets the discount limitation identifier.
        /// </summary>
        public int DiscountLimitationId { get; set; }

        /// <summary>
        /// Gets or sets the discount limitation.
        /// </summary>
        [NotMapped]
        public DiscountLimitationType DiscountLimitation
        {
            get => (DiscountLimitationType)DiscountLimitationId;
            set => DiscountLimitationId = (int)value;
        }

        /// <summary>
        /// Gets or sets the discount limitation times.
        /// It is used when the limitation type is set to "N times only" or "N times per customer".
        /// </summary>
        public int LimitationTimes { get; set; }

        /// <summary>
        /// Sets the discount remaining time (in hours) from which a countdown should be displayed in product detail,
        /// e.g. "ends in 3 hours, 23 min.". Only applies to limited time discounts with a non-null <see cref="EndDateUtc"/>.
        /// A value set here overwrites the system default <see cref="PriceSettings.ShowOfferCountdownRemainingHours"/>.
        /// </summary>
        public int? ShowCountdownRemainingHours { get; set; }

        /// <summary>
        /// The label of the discount badge, e.g. "Deal".
        /// A value set here overwrites the system default <see cref="PriceSettings.OfferBadgeLabel"/>
        /// or <see cref="PriceSettings.LimitedOfferBadgeLabel"/> accordingly.
        /// </summary>
        [LocalizedProperty]
        [StringLength(50)]
        public string OfferBadgeLabel { get; set; }

        private ICollection<RuleSetEntity> _ruleSets;
        /// <summary>
        /// Gets or sets assigned rule sets.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<RuleSetEntity> RuleSets
        {
            get => _ruleSets ?? LazyLoader.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
            protected set => _ruleSets = value;
        }

        private ICollection<Manufacturer> _appliedToManufacturers;
        /// <summary>
        /// Gets or sets the manufacturers to which the discount is applied.
        /// </summary>
        public ICollection<Manufacturer> AppliedToManufacturers
        {
            get => _appliedToManufacturers ?? LazyLoader.Load(this, ref _appliedToManufacturers) ?? (_appliedToManufacturers ??= new HashSet<Manufacturer>());
            protected set => _appliedToManufacturers = value;
        }

        private ICollection<Category> _appliedToCategories;
        /// <summary>
        /// Gets or sets the categories to which the discount is applied.
        /// </summary>
        public ICollection<Category> AppliedToCategories
        {
            get => _appliedToCategories ?? LazyLoader.Load(this, ref _appliedToCategories) ?? (_appliedToCategories ??= new HashSet<Category>());
            protected set => _appliedToCategories = value;
        }

        private ICollection<Product> _appliedToProducts;
        /// <summary>
        /// Gets or sets the products to which the discount is applied.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<Product> AppliedToProducts
        {
            get => _appliedToProducts ?? LazyLoader.Load(this, ref _appliedToProducts) ?? (_appliedToProducts ??= new HashSet<Product>());
            protected set => _appliedToProducts = value;
        }
    }
}
