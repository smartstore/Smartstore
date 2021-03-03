using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Contains data that is used to recalculate details of an order.
    /// </summary>
    public class AutoUpdateOrderItemContext
    {
        public static string InfoKey => "UpdateOrderItemContextInfo";

        /// <summary>
        /// [IN] Whether order item is new
        /// </summary>
        public bool IsNewOrderItem { get; set; }

        /// <summary>
        /// [IN] Order item
        /// </summary>
        public OrderItem OrderItem { get; set; }

        /// <summary>
        /// [IN] Whether to adjust the inventory
        /// </summary>
        public bool AdjustInventory { get; set; }

        /// <summary>
        /// [IN] Whether to update order totals if order is in pending state
        /// </summary>
        public bool UpdateTotals { get; set; }

        /// <summary>
        /// [IN] Whether to update reward points
        /// </summary>
        public bool UpdateRewardPoints { get; set; }

        /// <summary>
        /// [IN] Quantity old
        /// </summary>
        public int QuantityOld { get; set; }

        /// <summary>
        /// [IN] Quantity new
        /// </summary>
        public int QuantityNew { get; set; }

        /// <summary>
        /// [IN] Old price incl. tax.
        /// </summary>
        public Money? PriceInclTaxOld { get; set; }

        /// <summary>
        /// [IN] Old price excl. tax.
        /// </summary>
        public Money? PriceExclTaxOld { get; set; }

        /// <summary>
        /// [OUT] Inventory changes
        /// </summary>
        public AdjustInventoryResult Inventory { get; set; }

        /// <summary>
        /// [OUT] Reward points old
        /// </summary>
        public int RewardPointsOld { get; set; }

        /// <summary>
        /// [OUT] Reward points new
        /// </summary>
        public int RewardPointsNew { get; set; }

        /// <summary>
        /// The value to which the quantity amount has changed
        /// </summary>
        public int QuantityDelta => QuantityNew - QuantityOld;

        /// <summary>
        /// Returns quantity changed by factor if it is greater than 0, otherwise 1
        /// </summary>
        public decimal QuantityChangeFactor => QuantityOld != 0 ? QuantityNew / QuantityOld : 1.0M;

        public string ToString(ILocalizationService localizationService)
        {
            if (Inventory == null && RewardPointsOld == 0 && RewardPointsNew == 0)
                return string.Empty;

            string stockOld = null;
            string stockNew = null;

            if (Inventory != null && Inventory.HasClearStockQuantityResult)
            {
                stockOld = Inventory.StockQuantityOld.ToString();
                stockNew = Inventory.StockQuantityNew.ToString();
            }

            return localizationService.GetResource("Admin.Orders.OrderItem.Update.Info")
                .FormatWith(stockOld.NaIfEmpty(), stockNew.NaIfEmpty(), RewardPointsOld, RewardPointsNew);
        }
    }
}