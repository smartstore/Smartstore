using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Contains data that is used to recalculate details of an order.
    /// </summary>
    public class UpdateOrderDetailsContext
    {
        public static string InfoKey => "UpdateOrderDetailsContextInfo";

        /// <summary>
        /// Indicates whether to update the order item.
        /// </summary>
        public bool UpdateOrderItem { get; set; }

        /// <summary>
        /// Indicates whether to update order totals if order is in pending state.
        /// </summary>
        public bool UpdateTotals { get; set; }

        /// <summary>
        /// Indicates whether to update reward points.
        /// </summary>
        public bool UpdateRewardPoints { get; set; }

        /// <summary>
        /// Indicates whether to adjust the inventory.
        /// </summary>
        public bool AdjustInventory { get; set; }

        /// <summary>
        /// Result of adjusting the inventory.
        /// </summary>
        public AdjustInventoryResult Inventory { get; set; }

        public int? OldQuantity { get; set; }
        public int? NewQuantity { get; set; }
        public int ReduceQuantity { get; set; }

        public decimal? NewUnitPriceInclTax { get; set; }
        public decimal? NewUnitPriceExclTax { get; set; }
        public decimal? NewTaxRate { get; set; }
        public decimal? NewDiscountInclTax { get; set; }
        public decimal? NewDiscountExclTax { get; set; }

        public decimal? OldPriceInclTax { get; set; }
        public decimal? OldPriceExclTax { get; set; }
        public decimal? NewPriceInclTax { get; set; }
        public decimal? NewPriceExclTax { get; set; }

        public int OldRewardPoints { get; set; }
        public int NewRewardPoints { get; set; }

        public string ToString(ILocalizationService localizationService)
        {
            if (Inventory == null && OldRewardPoints == 0 && NewRewardPoints == 0)
                return string.Empty;

            string stockOld = null;
            string stockNew = null;

            if (Inventory != null && Inventory.HasClearStockQuantityResult)
            {
                stockOld = Inventory.StockQuantityOld.ToString();
                stockNew = Inventory.StockQuantityNew.ToString();
            }

            return localizationService.GetResource("Admin.Orders.OrderItem.Update.Info")
                .FormatInvariant(stockOld.NaIfEmpty(), stockNew.NaIfEmpty(), OldRewardPoints, NewRewardPoints);
        }
    }
}