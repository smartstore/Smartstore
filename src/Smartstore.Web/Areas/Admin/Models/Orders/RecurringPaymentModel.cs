namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.RecurringPayments.Fields.")]
    public class RecurringPaymentModel : EntityModelBase
    {
        [LocalizedDisplay("*CycleLength")]
        public int CycleLength { get; set; }

        [LocalizedDisplay("*CyclePeriod")]
        public int CyclePeriodId { get; set; }

        [LocalizedDisplay("*CyclePeriod")]
        public string CyclePeriodString { get; set; }

        [LocalizedDisplay("*TotalCycles")]
        public int TotalCycles { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime StartDate { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*NextPaymentDate")]
        public DateTime? NextPaymentDate { get; set; }

        [LocalizedDisplay("*CyclesRemaining")]
        public int CyclesRemaining { get; set; }

        [LocalizedDisplay("*InitialOrder")]
        public int InitialOrderId { get; set; }
        public string InitialOrderNumber { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }
        [LocalizedDisplay("*Customer")]
        public string CustomerFullName { get; set; }

        [LocalizedDisplay("*PaymentType")]
        public string PaymentType { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public bool CanCancel { get; set; }
        public string EditUrl { get; set; }
        public string CustomerEditUrl { get; set; }
        public string InitialOrderEditUrl { get; set; }

        public List<RecurringPaymentHistoryModel> History { get; set; } = new();

        [LocalizedDisplay("Admin.RecurringPayments.History.")]
        public class RecurringPaymentHistoryModel : EntityModelBase
        {
            public int RecurringPaymentId { get; set; }

            [LocalizedDisplay("*Order")]
            public int OrderId { get; set; }
            public string OrderNumber { get; set; }
            public string OrderEditUrl { get; set; }

            [LocalizedDisplay("*OrderStatus")]
            public string OrderStatus { get; set; }

            [LocalizedDisplay("*PaymentStatus")]
            public string PaymentStatus { get; set; }

            [LocalizedDisplay("*ShippingStatus")]
            public string ShippingStatus { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }
    }
}
