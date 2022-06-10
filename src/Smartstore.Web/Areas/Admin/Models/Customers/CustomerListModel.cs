using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.Customers.List.")]
    public class CustomerListModel : ModelBase
    {
        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("*CustomerRoles")]
        public int[] SearchCustomerRoleIds { get; set; }

        [LocalizedDisplay("*SearchEmail")]
        public string SearchEmail { get; set; }

        [LocalizedDisplay("*SearchUsername")]
        public string SearchUsername { get; set; }
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("*SearchTerm")]
        public string SearchTerm { get; set; }

        [LocalizedDisplay("*SearchCustomerNumber")]
        public string SearchCustomerNumber { get; set; }

        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchDayOfBirth { get; set; }
        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchMonthOfBirth { get; set; }
        [LocalizedDisplay("*SearchDateOfBirth")]
        public string SearchYearOfBirth { get; set; }
        public bool DateOfBirthEnabled { get; set; }

        public bool CompanyEnabled { get; set; }

        [LocalizedDisplay("*SearchPhone")]
        public string SearchPhone { get; set; }
        public bool PhoneEnabled { get; set; }

        [LocalizedDisplay("*SearchZipCode")]
        public string SearchZipPostalCode { get; set; }
        public bool ZipPostalCodeEnabled { get; set; }

        [LocalizedDisplay("*SearchActiveOnly")]
        public bool? SearchActiveOnly { get; set; }
    }
}
