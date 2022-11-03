namespace Smartstore.Admin.Models.Discounts
{
    [LocalizedDisplay("Admin.Promotions.Discounts.List.")]
    public class DiscountListModel
    {
        [LocalizedDisplay("*Name")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*DiscountType")]
        public int? SearchDiscountTypeId { get; set; }

        [LocalizedDisplay("*UsePercentage")]
        public bool? SearchUsePercentage { get; set; }

        [LocalizedDisplay("*RequiresCouponCode")]
        public bool? SearchRequiresCouponCode { get; set; }
    }
}
