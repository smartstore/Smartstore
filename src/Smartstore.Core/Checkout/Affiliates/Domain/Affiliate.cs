using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Common;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Affiliates
{
    public class AffiliateMap : IEntityTypeConfiguration<Affiliate>
    {
        public void Configure(EntityTypeBuilder<Affiliate> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(x => x.Address)
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents an affiliate
    /// </summary>
    public partial class Affiliate : EntityWithAttributes, ISoftDeletable
    {
        private readonly ILazyLoader _lazyLoader;

        public Affiliate()
        {
        }

        public Affiliate(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
            get => _lazyLoader?.Load(this, ref _address) ?? _address;
            set => _address = value;
        }
    }
}
