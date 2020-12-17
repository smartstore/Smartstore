using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Payment
{
    public class PaymentMethodMap : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder.HasMany(c => c.RuleSets)
                .WithMany(c => c.PaymentMethods)
                .UsingEntity(x => x.ToTable("RuleSet_PaymentMethod_Mapping"));
        }
    }

    /// <summary>
    /// Represents a payment method.
    /// </summary>
    public partial class PaymentMethod : BaseEntity, ILocalizedEntity, IStoreRestricted, IRulesContainer
    {
        private readonly ILazyLoader _lazyLoader;

        public PaymentMethod()
        {
        }

        public PaymentMethod(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the payment method system name.
        /// </summary>
        [Required, StringLength(4000)]
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets the full description.
        /// </summary>
        [StringLength(4000)]
        public string FullDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to round the order total. Also known as "Cash rounding".
        /// </summary>
        /// <see cref="https://en.wikipedia.org/wiki/Cash_rounding"/>
        public bool RoundOrderTotalEnabled { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        private ICollection<RuleSetEntity> _ruleSets;
        /// <summary>
        /// Gets or sets assigned rule sets.
        /// </summary>
        [JsonIgnore]
        public ICollection<RuleSetEntity> RuleSets
        {
            get => _lazyLoader?.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
            protected set => _ruleSets = value;
        }
    }
}
