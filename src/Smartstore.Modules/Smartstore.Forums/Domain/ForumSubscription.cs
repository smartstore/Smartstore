using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;
using Smartstore.Domain;

namespace Smartstore.Forums.Domain
{
    internal class ForumSubscriptionMap : IEntityTypeConfiguration<ForumSubscription>
    {
        public void Configure(EntityTypeBuilder<ForumSubscription> builder)
        {
            builder.HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a forum subscription item.
    /// </summary>
    [Table("Forums_Subscription")]
    [Index(nameof(ForumId), Name = "IX_Forums_Subscription_ForumId")]
    [Index(nameof(TopicId), Name = "IX_Forums_Subscription_TopicId")]
    public partial class ForumSubscription : BaseEntity
    {
        public ForumSubscription()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ForumSubscription(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the forum subscription identifier.
        /// </summary>
        public Guid SubscriptionGuid { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the forum identifier.
        /// </summary>
        public int ForumId { get; set; }

        /// <summary>
        /// Gets or sets the topic identifier.
        /// </summary>
        public int TopicId { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        private Customer _customer;
        /// <summary>
        /// Gets the customer.
        /// </summary>
        public virtual Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }
    }
}
