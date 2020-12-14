using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Discounts
{
    public class DiscountMap : IEntityTypeConfiguration<Discount>
    {
        public void Configure(EntityTypeBuilder<Discount> builder)
        {
            builder.Property(c => c.DiscountPercentage).HasPrecision(18, 4);
            builder.Property(c => c.DiscountAmount).HasPrecision(18, 4);

            builder.HasMany(c => c.AppliedToProducts)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity(x => x.ToTable("Discount_AppliedToProducts"));
        }
    }

    /// <summary>
    /// Represents a discount.
    /// </summary>
    /// TODO: (mg) (core): Implement IRulesContainer for discount entity.
    [DebuggerDisplay("{Name} - {DiscountType}")]
    public partial class Discount : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public Discount()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Discount(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
        /// Gets or sets a value indicating whether whether the discount amount is calculated by percentage.
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
        /// Gets or sets a value indicating whether discount requires a coupon code.
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

        /// TODO: (mg) (core): Implement discount navigation properties.

        private ICollection<Product> _products;
        /// <summary>
        /// Gets or sets the products to which the discount is applied.
        /// </summary>
        [JsonIgnore]
        public ICollection<Product> AppliedToProducts
        {
            get => _lazyLoader?.Load(this, ref _products) ?? (_products ??= new HashSet<Product>());
            protected set => _products = value;
        }
    }
}
