using System;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.OnlineCustomers.Fields.")]
    public class OnlineCustomerModel : EntityModelBase
    {
        [LocalizedDisplay("*CustomerInfo")]
        public string CustomerInfo { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string LastIpAddress { get; set; }

        [LocalizedDisplay("*Location")]
        public string Location { get; set; }

        [LocalizedDisplay("*LastActivityDate")]
        public DateTime LastActivityDate { get; set; }

        [LocalizedDisplay("*LastVisitedPage")]
        public string LastVisitedPage { get; set; }

        public string EditUrl { get; set; }
    }
}
