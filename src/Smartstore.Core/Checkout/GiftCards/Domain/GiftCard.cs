using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.GiftCards
{
    public class GiftCardMap : IEntityTypeConfiguration<GiftCard>
    {
        public void Configure(EntityTypeBuilder<GiftCard> builder)
        {   
            builder.HasOne(x => x.PurchasedWithOrderItem)
                .WithMany(x => x.AssociatedGiftCards)
                .HasForeignKey(x => x.PurchasedWithOrderItemId);
        }
    }

    /// <summary>
    /// Represents a gift card
    /// </summary>
    public partial class GiftCard : EntityWithAttributes
    {
        private readonly ILazyLoader _lazyLoader;

        public GiftCard()
        {
        }

        public GiftCard(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
        public string GiftCardCouponCode { get; set; }

        /// <summary>
        /// Gets or sets a recipient name
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Gets or sets a recipient email
        /// </summary>
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Gets or sets a sender name
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets a sender email
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets a message
        /// </summary>
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
            get => _lazyLoader?.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            protected set => _giftCardUsageHistory = value;
        }
                
        private OrderItem _purchasedWithOrderItem;
        /// <summary>
        /// Gets or sets the associated order item
        /// </summary>
        public OrderItem PurchasedWithOrderItem
        {
            get => _lazyLoader?.Load(this, ref _purchasedWithOrderItem) ?? _purchasedWithOrderItem;
            protected set => _purchasedWithOrderItem = value;
        }
    }
}
