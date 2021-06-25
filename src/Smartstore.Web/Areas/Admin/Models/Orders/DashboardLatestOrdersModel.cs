using System.Collections.Generic;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Admin.Models.Orders
{
    public class DashboardLatestOrdersModel
    {
        public IList<DashboardOrderModel> LatestOrders { get; set; } = new List<DashboardOrderModel>();
    }

    public class DashboardOrderModel
    {
        public int CustomerId { get; set; }
        public string CustomerDisplayName { get; set; }
        public int ProductsTotal { get; set; }
        public string TotalAmount { get; set; }
        public string Created { get; set; }
        public OrderStatus OrderState { get; set; }
        public int OrderId { get; set; }
    }
}
