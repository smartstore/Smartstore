using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Common;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Affiliates
{
    internal class AffiliateMap : IEntityTypeConfiguration<Affiliate>
    {
        public void Configure(EntityTypeBuilder<Affiliate> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(x => x.Address)
                .WithMany()
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    /// <summary>
    /// Represents an affiliate
    /// </summary>
    public partial class Affiliate : EntityWithAttributes, ISoftDeletable
    {
        public Affiliate()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Affiliate(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been (soft) deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the address identifier
        /// </summary>
        public int AddressId { get; set; }

        private Address _address;
        /// <summary>
        /// Gets or sets the address relating to the affiliate
        /// </summary>
        [JsonIgnore]
        public Address Address
        {
            get => _address ?? LazyLoader.Load(this, ref _address);
            set => _address = value;
        }
    }
}
