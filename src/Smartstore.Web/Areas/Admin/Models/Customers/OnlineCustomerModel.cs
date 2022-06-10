namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.OnlineCustomers.Fields.")]
    public class OnlineCustomerModel : EntityModelBase
    {
        [LocalizedDisplay("*CustomerInfo")]
        public string CustomerInfo { get; set; }

        [LocalizedDisplay("Account.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.Fields.Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string LastIpAddress { get; set; }

        [LocalizedDisplay("*Location")]
        public string Location { get; set; }

        [LocalizedDisplay("*LastActivityDate")]
        public DateTime LastActivityDate { get; set; }

        [LocalizedDisplay("*LastVisitedPage")]
        public string LastVisitedPage { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public string EditUrl { get; set; }
    }
}
