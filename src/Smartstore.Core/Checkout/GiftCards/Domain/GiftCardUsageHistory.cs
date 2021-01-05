using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.GiftCards
{
    public class GiftCardUsageHistoryMap : IEntityTypeConfiguration<GiftCardUsageHistory>
    {
        public void Configure(EntityTypeBuilder<GiftCardUsageHistory> builder)
        {
            builder.HasOne(x => x.GiftCard)
                .WithMany(x => x.GiftCardUsageHistory)
                .HasForeignKey(x => x.GiftCardId);

            builder.HasOne(x => x.UsedWithOrder)
                .WithMany(x => x.GiftCardUsageHistory)
                .HasForeignKey(x => x.UsedWithOrderId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents a gift card usage history entry
    /// </summary>
    [Index(nameof(GiftCardId), Name = "IX_GiftCardId")]
    [Index(nameof(UsedWithOrderId), Name = "IX_UsedWithOrderId")]
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
        [JsonIgnore]
        public GiftCard GiftCard
        {
            get => _lazyLoader?.Load(this, ref _giftCard) ?? _giftCard;
            set => _giftCard = value;
        }

        private Order _usedWithOrder;
        /// <summary>
        /// Gets the order associated with the gift card
        /// </summary>
        [JsonIgnore]
        public Order UsedWithOrder
        {
            get => _lazyLoader?.Load(this, ref _usedWithOrder) ?? _usedWithOrder;
            set => _usedWithOrder = value;
        }
    }
}