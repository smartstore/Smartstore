namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.")]
    public class UpdateOrderItemModel : EntityModelBase
    {
        public string Caption { get; set; }
        public string PostUrl { get; set; }
        public bool ShowUpdateRewardPoints { get; set; }
        public bool ShowUpdateTotals { get; set; }

        [LocalizedDisplay("*AdjustInventory")]
        public bool AdjustInventory { get; set; } = true;

        [LocalizedDisplay("*UpdateRewardPoints")]
        public bool UpdateRewardPoints { get; set; }

        [LocalizedDisplay("*UpdateTotals")]
        public bool UpdateTotals { get; set; }

        public int? NewQuantity { get; set; }
        public decimal? NewUnitPriceInclTax { get; set; }
        public decimal? NewUnitPriceExclTax { get; set; }
        public decimal? NewTaxRate { get; set; }
        public decimal? NewDiscountInclTax { get; set; }
        public decimal? NewDiscountExclTax { get; set; }
        public decimal? NewPriceInclTax { get; set; }
        public decimal? NewPriceExclTax { get; set; }
    }
}
