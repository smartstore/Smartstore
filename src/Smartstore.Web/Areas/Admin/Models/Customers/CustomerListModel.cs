using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.Customers.List.")]
    public class CustomerListModel : CustomerSearchModel
    {
        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("*CustomerRoles")]
        public int[] SearchCustomerRoleIds { get; set; }

        [LocalizedDisplay("Admin.Common.Search.StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("Admin.Common.Search.EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchDayOfBirth { get; set; }
        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchMonthOfBirth { get; set; }
        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchYearOfBirth { get; set; }
        public bool DateOfBirthEnabled { get; set; }

        public bool UsernamesEnabled { get; set; }
        public bool CompanyEnabled { get; set; }

        [LocalizedDisplay("*SearchPhone")]
        public string SearchPhone { get; set; }
        public bool PhoneEnabled { get; set; }

        [LocalizedDisplay("*SearchZipCode")]
        public string SearchZipPostalCode { get; set; }
        public bool ZipPostalCodeEnabled { get; set; }
        public bool IsSingleStoreMode { get; set; }
    }
}
