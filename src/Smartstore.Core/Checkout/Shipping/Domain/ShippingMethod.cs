using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Shipping
{
    internal class ShippingMethodMap : IEntityTypeConfiguration<ShippingMethod>
    {
        public void Configure(EntityTypeBuilder<ShippingMethod> builder)
        {
            builder
                .HasMany(x => x.RuleSets)
                .WithMany(x => x.ShippingMethods)
                .UsingEntity<Dictionary<string, object>>(
                    "RuleSet_ShippingMethod_Mapping",
                    c => c
                        .HasOne<RuleSetEntity>()
                        .WithMany()
                        .HasForeignKey("RuleSetEntity_Id")
                        .HasConstraintName("FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<ShippingMethod>()
                        .WithMany()
                        .HasForeignKey("ShippingMethod_Id")
                        .HasConstraintName("FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_ShippingMethod_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c.HasKey("ShippingMethod_Id", "RuleSetEntity_Id"));
        }
    }

    /// <summary>
    /// Represents a shipping method.
    /// </summary>
    [CacheableEntity]
    public partial class ShippingMethod : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IDisplayOrder, IRulesContainer
    {
        private readonly ILazyLoader _lazyLoader;

        public ShippingMethod()
        {
        }

        public ShippingMethod(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(4000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore charges.
        /// </summary>
        public bool IgnoreCharges { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        private ICollection<RuleSetEntity> _ruleSets;
        /// <summary>
        /// Gets or sets assigned rule sets.
        /// </summary>
        [JsonIgnore]
        public ICollection<RuleSetEntity> RuleSets
        {
            get => _ruleSets = _lazyLoader?.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
            protected set => _ruleSets = value;
        }
    }
}