using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Messages
{
    public class NewsletterSubscriptionListModel
    {
        [LocalizedDisplay("Admin.Customers.Customers.List.SearchEmail")]
        public string SearchEmail { get; set; }

        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Customers.Customers.List.CustomerRoles")]
        public int[] SearchCustomerRoleIds { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
    }
}
