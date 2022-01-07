namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.GiftCards.List.")]
    public class GiftCardListModel : ModelBase
    {
        [LocalizedDisplay("*CouponCode")]
        public string CouponCode { get; set; }

        [LocalizedDisplay("*Activated")]
        public bool? Activated { get; set; }
    }
}
