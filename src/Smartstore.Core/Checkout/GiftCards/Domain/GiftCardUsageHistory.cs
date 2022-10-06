using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.GiftCards
{
    internal class GiftCardUsageHistoryMap : IEntityTypeConfiguration<GiftCardUsageHistory>
    {
        public void Configure(EntityTypeBuilder<GiftCardUsageHistory> builder)
        {
            builder.HasOne(x => x.GiftCard)
                .WithMany(x => x.GiftCardUsageHistory)
                .HasForeignKey(x => x.GiftCardId);

            builder.HasOne(x => x.UsedWithOrder)
                .WithMany(x => x.GiftCardUsageHistory)
                .HasForeignKey(x => x.UsedWithOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a gift card usage history entry
    /// </summary>
    public partial class GiftCardUsageHistory : BaseEntity
    {
        public GiftCardUsageHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private GiftCardUsageHistory(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the gift card identifier
        /// </summary>
        public int GiftCardId { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int UsedWithOrderId { get; set; }

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
        [IgnoreDataMember]
        public GiftCard GiftCard
        {
            get => _giftCard ?? LazyLoader.Load(this, ref _giftCard);
            set => _giftCard = value;
        }

        private Order _usedWithOrder;
        /// <summary>
        /// Gets the order associated with the gift card
        /// </summary>
        [IgnoreDataMember]
        public Order UsedWithOrder
        {
            get => _usedWithOrder ?? LazyLoader.Load(this, ref _usedWithOrder);
            set => _usedWithOrder = value;
        }
    }
}