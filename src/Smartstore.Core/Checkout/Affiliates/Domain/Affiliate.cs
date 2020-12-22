using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Common;
using Smartstore.Domain;
using System.ComponentModel.DataAnnotations;

namespace Smartstore.Core.Checkout.Affiliates
{
    public class AffiliateMap : IEntityTypeConfiguration<Affiliate>
    {
        public void Configure(EntityTypeBuilder<Affiliate> builder)
        {
            // Globally exclude soft-deleted entities from all queries
            builder.HasQueryFilter(x => !x.Deleted);

            builder.HasOne(a => a.Address)
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents an affiliate
    /// </summary>
    public class Affiliate : BaseEntity, ISoftDeletable
    {
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

        /// <summary>
        /// Gets or sets the address relating to the affiliate
        /// </summary>
        [JsonIgnore, Required]
        public virtual Address Address { get; set; }
    }
}
