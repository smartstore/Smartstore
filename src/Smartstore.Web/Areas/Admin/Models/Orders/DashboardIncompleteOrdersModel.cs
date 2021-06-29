using Smartstore.Core.Common;
using Smartstore.Web.Modelling;
using System.Collections.Generic;

namespace Smartstore.Admin.Models.Orders
{
    public class DashboardIncompleteOrdersModel : ModelBase
    {
        public DashboardIncompleteOrdersModel()
        {
            Data = new List<DashboardIncompleteOrdersData>()
            {
                // NotShipped = 0 
                new DashboardIncompleteOrdersData(),
                // NotPaid = 1
                new DashboardIncompleteOrdersData(),
                // NewOrders = 2 
                new DashboardIncompleteOrdersData()
            };
        }

        public List<DashboardIncompleteOrdersData> Data { get; set; }
        public decimal Amount { get; set; }
        public Money AmountTotal { get; set; }
        public int Quantity { get; set; }
        public string QuantityTotal { get; set; }
    }

    public class DashboardIncompleteOrdersData
    {
        public decimal Amount { get; set; }
        public Money AmountFormatted { get; set; }
        public int Quantity { get; set; }
        public string QuantityFormatted { get; set; }
    }
}
