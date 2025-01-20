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
        public string StartedOnString
            => StartDate.ToString("g");

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*NextPaymentDate")]
        public DateTime? NextPaymentDate { get; set; }
        public DateTime? NextPaymentDateUtc { get; set; }

        public string NextPaymentDateString
            => NextPaymentDate?.ToString("g");

        public string NextPaymentDateFriendly
            => NextPaymentDate?.ToHumanizedString(false);

        public string NextPaymentLabelClass
        {
            get
            {
                if (NextPaymentDateUtc != null)
                {
                    var now = DateTime.UtcNow;
                    var dt = NextPaymentDateUtc.Value;

                    // Do not make the next payment yet.
                    var color = "danger";

                    if (dt <= now)
                    {
                        // Make the next payment.
                        color = "success";
                    }
                    else if (dt.Day == now.Day && dt.Month == now.Month && dt.Year == now.Year)
                    {
                        // Possibly make the next payment.
                        color = "warning";
                    }

                    return $"fa fa-fw fa-circle text-{color}";
                }
                 
                return "fa fa-fw icon-active-false";
            }
        }

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

        public bool CanProcessNextPayment
            => IsActive && NextPaymentDate != null;

        public string EditUrl { get; set; }
        public string CustomerEditUrl { get; set; }
        public string InitialOrderEditUrl { get; set; }

        public List<RecurringPaymentHistoryModel> History { get; set; } = [];
    }
}
