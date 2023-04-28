namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.Countries.Fields.")]
    public class CountrySearchListModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*TwoLetterIsoCode")]
        public string SearchTwoLetterIsoCode { get; set; }

        [LocalizedDisplay("*AllowsBilling")]
        public bool? SearchAllowsBilling { get; set; }

        [LocalizedDisplay("*AllowsShipping")]
        public bool? SearchAllowsShipping { get; set; }

        [LocalizedDisplay("*SubjectToVat")]
        public bool? SearchSubjectToVat { get; set; }

        [LocalizedDisplay("*Published")]
        public bool? SearchPublished { get; set; }
    }
}
