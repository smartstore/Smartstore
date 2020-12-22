using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Core.Checkout.Orders
{
    public class OrderMap : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);
        }
    }

    public partial class Order : EntityWithAttributes, IAuditable, ISoftDeletable
    {
        private readonly ILazyLoader _lazyLoader;

        public Order()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Order(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time of order creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time when order was updated
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        private ICollection<GiftCardUsageHistory> _giftCardUsageHistory;
        /// <summary>
        /// Gets or sets gift card usage history (gift cards applied within this order)
        /// </summary>
        public ICollection<GiftCardUsageHistory> GiftCardUsageHistory
        {
            get => _lazyLoader?.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            init => _giftCardUsageHistory = value;
        }
    }
}
