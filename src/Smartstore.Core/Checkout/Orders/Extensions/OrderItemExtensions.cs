using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static partial class OrderItemExtensions
    {
        /// <summary>
        /// Gets bundle items data for an order item.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>List of bundle items order data.</returns>
        public static List<ProductBundleItemOrderData> GetBundleData(this OrderItem orderItem)
        {
            if (orderItem != null && orderItem.BundleData.HasValue())
            {
                var data = orderItem.BundleData.Convert<List<ProductBundleItemOrderData>>();
                return data;
            }

            return new List<ProductBundleItemOrderData>();
        }

        /// <summary>
        /// Sets the bundle data for an order item.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <param name="bundleData">List of bundle items order data.</param>
        public static void SetBundleData(this OrderItem orderItem, List<ProductBundleItemOrderData> bundleData)
        {
            string rawData = null;

            if (bundleData != null && bundleData.Count > 0)
            {
                rawData = bundleData.Convert<string>();
            }

            orderItem.BundleData = rawData;
        }
    }
}
