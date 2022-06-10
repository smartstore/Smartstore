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
        // INFO: (mh) (core) For future consideration: with the Money struct, any "Amount as decimal" property gets obsolete, because Money is a smart container for both raw amount and formatted string.
        public decimal Amount { get; set; }
        public Money AmountTotal { get; set; }
        public int Quantity { get; set; }
        public string QuantityTotal { get; set; }
    }

    public class DashboardIncompleteOrdersData
    {
        // INFO: (mh) (core) Please see comment above
        public decimal Amount { get; set; }
        public Money AmountFormatted { get; set; }
        public int Quantity { get; set; }
        public string QuantityFormatted { get; set; }
    }
}
