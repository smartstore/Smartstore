using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Models.Customers
{
    public partial class CustomerOrderListModel : ModelBase
    {
        public IPagedList<OrderDetailsModel> Orders { get; set; }
        public IPagedList<RecurringPaymentModel> RecurringPayments { get; set; }

        public int? OrdersPage { get; set; }
        public int? RecurringPaymentsPage { get; set; }

        public partial class OrderDetailsModel : EntityModelBase
        {
            public string OrderNumber { get; set; }
            public Money OrderTotal { get; set; }
            public bool IsReturnRequestAllowed { get; set; }
            public string OrderStatus { get; set; }
            public DateTime CreatedOn { get; set; }
        }

        public partial class RecurringPaymentModel : EntityModelBase
        {
            public int InitialOrderId { get; set; }
            public string InitialOrderNumber { get; set; }
            public DateTime StartDate { get; set; }

            public int CyclesRemaining { get; set; }
            public DateTime? NextPayment { get; set; }

            public int CycleLength { get; set; }
            public RecurringProductCyclePeriod CyclePeriod { get; set; }
            public string CyclePeriodString { get; set; }
            public int TotalCycles { get; set; }

            public bool CanCancel { get; set; }
        }
    }
}
