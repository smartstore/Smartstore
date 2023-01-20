using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.Customers.List.")]
    public partial class CustomerSearchModel : ModelBase
    {
        [LocalizedDisplay("*SearchEmail")]
        public string SearchEmail { get; set; }

        [LocalizedDisplay("*SearchUsername")]
        public string SearchUsername { get; set; }

        [LocalizedDisplay("*SearchTerm")]
        public string SearchTerm { get; set; }

        [LocalizedDisplay("*SearchCustomerNumber")]
        public string SearchCustomerNumber { get; set; }

        [LocalizedDisplay("*SearchActiveOnly")]
        public bool? SearchActiveOnly { get; set; }
    }
}
