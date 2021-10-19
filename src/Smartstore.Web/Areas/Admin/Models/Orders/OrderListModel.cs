using System;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.List.")]
    public class OrderListModel : ModelBase
    {
        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*OrderStatus")]
        public string OrderStatusIds { get; set; }

        [LocalizedDisplay("*PaymentStatus")]
        public string PaymentStatusIds { get; set; }

        [LocalizedDisplay("*ShippingStatus")]
        public string ShippingStatusIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }

        [LocalizedDisplay("Order.PaymentMethod")]
        public string PaymentMethods { get; set; }

        [LocalizedDisplay("*OrderGuid")]
        public string OrderGuid { get; set; }

        [LocalizedDisplay("*OrderNumber")]
        public string OrderNumber { get; set; }

        [LocalizedDisplay("*GoDirectlyToNumber")]
        public string GoDirectlyToNumber { get; set; }

        // ProductId is only filled in context of product details (orders).
        // It is empty (null) in orders list.
        public int? ProductId { get; set; }
    }
}
