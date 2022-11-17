using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Checkout.Payment
{
    internal class PaymentMethodMap : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder
                .HasMany(c => c.RuleSets)
                .WithMany(c => c.PaymentMethods)
                .UsingEntity<Dictionary<string, object>>(
                    "RuleSet_PaymentMethod_Mapping",
                    c => c
                        .HasOne<RuleSetEntity>()
                        .WithMany()
                        .HasForeignKey("RuleSetEntity_Id")
                        .HasConstraintName("FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<PaymentMethod>()
                        .WithMany()
                        .HasForeignKey("PaymentMethod_Id")
                        .HasConstraintName("FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMethod_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("PaymentMethod_Id");
                        c.HasKey("PaymentMethod_Id", "RuleSetEntity_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a payment method.
    /// </summary>
    [CacheableEntity]
    [Index(nameof(PaymentMethodSystemName))]
    public partial class PaymentMethod : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IRulesContainer
    {
        public PaymentMethod()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PaymentMethod(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the payment method system name.
        /// </summary>
        /// <example>Payments.PayPalStandard</example>
        [Required, StringLength(255)]
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets the full description.
        /// </summary>
        [StringLength(4000)]
        public string FullDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to round the order total. Also known as "Cash rounding".
        /// <see href="https://en.wikipedia.org/wiki/Cash_rounding"/>.
        /// </summary>
        public bool RoundOrderTotalEnabled { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

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
    }
}