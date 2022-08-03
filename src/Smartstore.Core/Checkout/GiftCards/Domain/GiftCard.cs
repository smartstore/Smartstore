using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
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
        public GiftCard()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private GiftCard(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

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

        /// <summary>
        /// Gets or sets a recipient name
        /// </summary>
        [StringLength(450)]
        public string RecipientName { get; set; }

        /// <summary>
        /// Gets or sets a recipient email
        /// </summary>
        [StringLength(255)]
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Gets or sets a sender name
        /// </summary>
        [StringLength(450)]
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets a sender email
        /// </summary>
        [StringLength(255)]
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets a message
        /// </summary>
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