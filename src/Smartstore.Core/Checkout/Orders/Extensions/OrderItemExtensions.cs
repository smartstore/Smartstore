using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore;

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

        return [];
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

    /// <summary>
    /// Gets the maximum quantity that can be returned for a given order item,
    /// taking into account any existing return cases for the item.
    /// </summary>
    /// <param name="quantity">Desired return quantity. <c>null</c> to use the quantity of the order item.</param>
    public static int GetMaxReturnQuantity(this OrderItem orderItem, int? quantity = null)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        var returnCases = Guard.NotNull(orderItem?.Order?.Customer?.ReturnCases);

        var returnedQuantity = returnCases
            .Where(x => x.OrderItemId == orderItem.Id)
            .Sum(x => x.Quantity);

        return Math.Max((quantity ?? orderItem.Quantity) - returnedQuantity, 0);
    }
}