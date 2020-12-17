using Microsoft.EntityFrameworkCore;
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
            builder.Property(history => history.UsedValue).HasPrecision(18, 4);
            //this.Property(history => history.UsedValueInCustomerCurrency).HasPrecision(18, 4);

            builder.HasOne(history => history.GiftCard)
                .WithMany(history => history.GiftCardUsageHistory)
                .HasForeignKey(history => history.GiftCardId);

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

        /// <summary>
        /// Gets or sets the gift card
        /// </summary>
        public virtual GiftCard GiftCard { get; set; }

        /// <summary>
        /// Gets the gift card
        /// </summary>
        public virtual Order Order { get; set; }
    }
}
