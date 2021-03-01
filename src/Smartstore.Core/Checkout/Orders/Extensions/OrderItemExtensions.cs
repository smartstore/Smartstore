using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;

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

        /// <summary>
        /// Gets the total number of items which can be added to new shipments.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of items which can be added to new shipments.</returns>
        public static int GetItemsCanBeAddedToShipmentCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            var itemsCount = orderItem.GetShipmentItemsCount();

            return Math.Max(orderItem.Quantity - itemsCount, 0);
        }

        /// <summary>
        /// Gets the total number of items in all shipments.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of items in all shipments.</returns>
        public static int GetShipmentItemsCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            return SumUpQuantity(orderItem.Order.Shipments, orderItem);
        }

        /// <summary>
        /// Gets the total number of dispatched items.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of dispatched items.</returns>
        public static int GetDispatchedItemsCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            return SumUpQuantity(
                orderItem.Order.Shipments.Where(x => x.ShippedDateUtc.HasValue),
                orderItem);
        }

        /// <summary>
        /// Gets the total number of not dispatched items.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of not dispatched items.</returns>
        public static int GetNotDispatchedItemsCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            return SumUpQuantity(
                orderItem.Order.Shipments.Where(x => !x.ShippedDateUtc.HasValue),
                orderItem);
        }

        /// <summary>
        /// Gets the total number of already delivered items.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of already delivered items.</returns>
        public static int GetDeliveredItemsCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            return SumUpQuantity(
                orderItem.Order.Shipments.Where(x => x.DeliveryDateUtc.HasValue), 
                orderItem);
        }

        /// <summary>
        /// Gets the total number of not delivered items.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Total number of already delivered items.</returns>
        public static int GetNotDeliveredItemsCount(this OrderItem orderItem)
        {
            Guard.NotNull(orderItem, nameof(orderItem));

            return SumUpQuantity(
                orderItem.Order.Shipments.Where(x => !x.DeliveryDateUtc.HasValue),
                orderItem);
        }

        private static int SumUpQuantity(IEnumerable<Shipment> shipments, OrderItem orderItem)
        {
            var result = 0;

            foreach (var shipment in shipments)
            {
                var item = shipment.ShipmentItems.FirstOrDefault(x => x.OrderItemId == orderItem.Id);
                if (item != null)
                {
                    result += item.Quantity;
                }
            }

            return result;
        }
    }
}
