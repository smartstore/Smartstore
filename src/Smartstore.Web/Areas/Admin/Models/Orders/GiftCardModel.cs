using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.GiftCards.Fields.")]
    public class GiftCardModel : EntityModelBase
    {
        [LocalizedDisplay("*GiftCardType")]
        public int GiftCardTypeId { get; set; }

        [LocalizedDisplay("*Order")]
        public int? PurchasedWithOrderId { get; set; }

        [LocalizedDisplay("*Amount")]
        public decimal Amount { get; set; }

        [LocalizedDisplay("*Amount")]
        public string AmountStr { get; set; }

        [LocalizedDisplay("*RemainingAmount")]
        public string RemainingAmountStr { get; set; }

        [LocalizedDisplay("*IsGiftCardActivated")]
        public bool IsGiftCardActivated { get; set; }

        [LocalizedDisplay("*GiftCardCouponCode")]
        public string GiftCardCouponCode { get; set; }

        [LocalizedDisplay("*RecipientName")]
        public string RecipientName { get; set; }

        [LocalizedDisplay("*RecipientEmail")]
        public string RecipientEmail { get; set; }

        [LocalizedDisplay("*SenderName")]
        public string SenderName { get; set; }

        [LocalizedDisplay("*SenderEmail")]
        public string SenderEmail { get; set; }

        [UIHint("Textarea"), AdditionalMetadata("rows", 6)]
        [LocalizedDisplay("*Message")]
        public string Message { get; set; }

        [LocalizedDisplay("*IsRecipientNotified")]
        public bool IsRecipientNotified { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*Language")]
        public int LanguageId { get; set; }

        public string EditUrl { get; set; }
    }

    [LocalizedDisplay("Admin.GiftCards.History.")]
    public class GiftCardUsageHistoryModel : EntityModelBase
    {
        [LocalizedDisplay("*UsedValue")]
        public string UsedValue { get; set; }

        [LocalizedDisplay("*Order")]
        public int OrderId { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public string OrderEditUrl { get; set; }
        public string OrderEditLinkText { get; set; }
    }
}
