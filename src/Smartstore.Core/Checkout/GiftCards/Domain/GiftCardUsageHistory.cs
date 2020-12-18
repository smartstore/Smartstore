using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;
using System;

namespace Smartstore.Core.Checkout.GiftCards
{
    public class GiftCardUsageHistoryMap : IEntityTypeConfiguration<GiftCardUsageHistory>
    {
        public void Configure(EntityTypeBuilder<GiftCardUsageHistory> builder)
        {
            builder.Property(x => x.UsedValue).HasPrecision(18, 4);
            //this.Property(history => history.UsedValueInCustomerCurrency).HasPrecision(18, 4);

            builder.HasOne(x => x.GiftCard)
                .WithMany(x => x.GiftCardUsageHistory)
                .HasForeignKey(x => x.GiftCardId);

            // TODO: (core) (ms) Order.GiftCardUsageHistory is needed
            //builder.HasOne(history => history.Order)
            //    .WithMany(history => history.GiftCardUsageHistory)
            //    .HasForeignKey(history => history.UsedWithOrderId);
        }
    }

    /// <summary>
    /// Represents a gift card usage history entry
    /// </summary>
    public partial class GiftCardUsageHistory : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public GiftCardUsageHistory()
        {
        }

        public GiftCardUsageHistory(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the gift card identifier
        /// </summary>
        public int GiftCardId { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the used value (amount)
        /// </summary>
        public decimal UsedValue { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private GiftCard _giftCard;
        /// <summary>
        /// Gets or sets the gift card
        /// </summary>
        public GiftCard GiftCard
        {
            get => _lazyLoader?.Load(this, ref _giftCard) ?? _giftCard;
            set => _giftCard = value;
        }

        private Order _order;
        /// <summary>
        /// Gets the order associated with the gift card
        /// </summary>
        public Order Order
        {
            get => _lazyLoader?.Load(this, ref _order) ?? _order;
            set => _order = value;
        }
    }
}
