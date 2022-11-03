namespace Smartstore.Admin.Models.Discounts
{
    public class DiscountAppliedToEntityModel : EntityModelBase
    {
        public string Name { get; set; }
    }

    public class DiscountUsageHistoryModel : EntityModelBase
    {
        public int DiscountId { get; set; }
        public string OrderEditUrl { get; set; }
        public string OrderEditLinkText { get; set; }

        [LocalizedDisplay("Admin.Promotions.Discounts.History.Order")]
        public int OrderId { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOnUtc { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }
    }
}
