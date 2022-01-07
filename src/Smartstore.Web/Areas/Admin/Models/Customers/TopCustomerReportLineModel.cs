namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class TopCustomerReportLineModel : ModelBase
    {
        [LocalizedDisplay("Admin.Customers.Reports.BestBy.Fields.OrderTotal")]
        public Money OrderTotal { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.Fields.OrderCount")]
        public string OrderCount { get; set; }

        public string CustomerDisplayName { get; set; }

        [LocalizedDisplay("Admin.Common.Entity.Fields.Id")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("Account.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*LastActivityDate")]
        public DateTime LastActivityDate { get; set; }

        public string EditUrl { get; set; }
    }
}
