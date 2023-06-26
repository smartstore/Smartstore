using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.GiftCards
{
    internal class GiftCardMap : IEntityTypeConfiguration<GiftCard>
    {
        public void Configure(EntityTypeBuilder<GiftCard> builder)
        {
            builder.HasOne(x => x.PurchasedWithOrderItem)
                .WithMany(x => x.AssociatedGiftCards)
                .HasForeignKey(x => x.PurchasedWithOrderItemId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a gift card
    /// </summary>
    public partial class GiftCard : EntityWithAttributes, IGiftCardInfo
    {
        /// <summary>
        /// Gets or sets the gift card type identifier
        /// </summary>
        public int GiftCardTypeId { get; set; }

        /// <summary>
        /// Gets or sets the gift card type
        /// </summary>
        [NotMapped]
        public GiftCardType GiftCardType
        {
            get => (GiftCardType)GiftCardTypeId;
            set => GiftCardTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets the associated order item identifier
        /// </summary>
        public int? PurchasedWithOrderItemId { get; set; }

        /// <summary>
        /// Gets or sets the amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gift card is activated
        /// </summary>
        public bool IsGiftCardActivated { get; set; }

        /// <summary>
        /// Gets or sets a gift card coupon code
        /// </summary>
        [StringLength(100)]
        public string GiftCardCouponCode { get; set; }

        /// <inheritdoc/>
        [StringLength(450)]
        public string RecipientName { get; set; }

        /// <inheritdoc/>
        [StringLength(255)]
        public string RecipientEmail { get; set; }

        /// <inheritdoc/>
        [StringLength(450)]
        public string SenderName { get; set; }

        /// <inheritdoc/>
        [StringLength(255)]
        public string SenderEmail { get; set; }

        /// <inheritdoc/>
        [StringLength(4000)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether recipient is notified
        /// </summary>
        public bool IsRecipientNotified { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private ICollection<GiftCardUsageHistory> _giftCardUsageHistory;
        /// <summary>
        /// Gets or sets the gift card usage history
        /// </summary> 
        public ICollection<GiftCardUsageHistory> GiftCardUsageHistory
        {
            get => _giftCardUsageHistory ?? LazyLoader.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            protected set => _giftCardUsageHistory = value;
        }

        private OrderItem _purchasedWithOrderItem;
        /// <summary>
        /// Gets or sets the associated order item
        /// </summary>
        public OrderItem PurchasedWithOrderItem
        {
            get => _purchasedWithOrderItem ?? LazyLoader.Load(this, ref _purchasedWithOrderItem);
            set => _purchasedWithOrderItem = value;
        }
    }
}