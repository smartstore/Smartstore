using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Customers
{
    [LocalizedDisplay("Account.Fields.")]
    public partial class CustomerInfoModel : TabbableModel
    {
        [LocalizedDisplay("*Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [LocalizedDisplay("*CustomerNumber")]
        public string CustomerNumber { get; set; }
        public bool CustomerNumberEnabled { get; set; }
        public bool DisplayCustomerNumber { get; set; }

        public bool CheckUsernameAvailabilityEnabled { get; set; }
        public bool AllowUsersToChangeUsernames { get; set; }
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        // Form fields & properties.
        public bool GenderEnabled { get; set; }
        [LocalizedDisplay("*Gender")]
        public string Gender { get; set; }

        public bool TitleEnabled { get; set; }
        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        public bool FirstNameRequired { get; set; }
        [LocalizedDisplay("*FirstName")]
        public string FirstName { get; set; }

        public bool LastNameRequired { get; set; }
        [LocalizedDisplay("*LastName")]
        public string LastName { get; set; }

        public bool DateOfBirthEnabled { get; set; }

        [LocalizedDisplay("*DateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        public bool CompanyEnabled { get; set; }
        public bool CompanyRequired { get; set; }
        [LocalizedDisplay("*Company")]
        public string Company { get; set; }

        public bool StreetAddressEnabled { get; set; }
        public bool StreetAddressRequired { get; set; }
        [LocalizedDisplay("*StreetAddress")]
        public string StreetAddress { get; set; }

        public bool StreetAddress2Enabled { get; set; }
        public bool StreetAddress2Required { get; set; }
        [LocalizedDisplay("*StreetAddress2")]
        public string StreetAddress2 { get; set; }

        public bool ZipPostalCodeEnabled { get; set; }
        public bool ZipPostalCodeRequired { get; set; }
        [LocalizedDisplay("*ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public bool CityEnabled { get; set; }
        public bool CityRequired { get; set; }
        [LocalizedDisplay("*City")]
        public string City { get; set; }

        public bool CountryEnabled { get; set; }
        [LocalizedDisplay("*Country")]
        public int CountryId { get; set; }

        public bool StateProvinceEnabled { get; set; }
        [LocalizedDisplay("*StateProvince")]
        public int StateProvinceId { get; set; }

        public bool PhoneEnabled { get; set; }
        public bool PhoneRequired { get; set; }
        [LocalizedDisplay("*Phone")]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        public bool FaxEnabled { get; set; }
        public bool FaxRequired { get; set; }
        [LocalizedDisplay("*Fax")]
        [DataType(DataType.PhoneNumber)]
        public string Fax { get; set; }

        public bool NewsletterEnabled { get; set; }
        [LocalizedDisplay("*Newsletter")]
        public bool Newsletter { get; set; }

        [LocalizedDisplay("*TimeZone")]
        public string TimeZoneId { get; set; }
        public bool AllowCustomersToSetTimeZone { get; set; }

        // EU VAT
        [LocalizedDisplay("*VatNumber")]
        public string VatNumber { get; set; }
        public string VatNumberStatusNote { get; set; }
        public bool DisplayVatNumber { get; set; }

        [LocalizedDisplay("Account.AssociatedExternalAuth")]
        public List<AssociatedExternalAuthModel> AssociatedExternalAuthRecords { get; set; } = [];

        [LocalizedDisplay("*PreferredShippingMethod")]
        public int? PreferredShippingMethodId { get; set; }

        [LocalizedDisplay("*PreferredPaymentMethod")]
        public string PreferredPaymentMethod { get; set; }

        public partial class AssociatedExternalAuthModel : EntityModelBase
        {
            public string Email { get; set; }
            public string ExternalIdentifier { get; set; }
            public string AuthMethodName { get; set; }
        }
    }

    public class CustomerInfoValidator : SmartValidator<CustomerInfoModel>
    {
        public CustomerInfoValidator(Localizer T, CustomerSettings customerSettings)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();

            //form fields
            if (customerSettings.FirstNameRequired)
            {
                RuleFor(x => x.FirstName).NotEmpty();
            }

            RuleFor(x => x.FirstName).ValidName(T);

            if (customerSettings.LastNameRequired)
            {
                RuleFor(x => x.LastName).NotEmpty();
            }

            RuleFor(x => x.LastName).ValidName(T);

            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            {
                RuleFor(x => x.Company).NotEmpty();
            }
            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            {
                RuleFor(x => x.StreetAddress).NotEmpty();
            }
            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            {
                RuleFor(x => x.StreetAddress2).NotEmpty();
            }
            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            {
                RuleFor(x => x.ZipPostalCode).NotEmpty();
            }
            if (customerSettings.CityRequired && customerSettings.CityEnabled)
            {
                RuleFor(x => x.City).NotEmpty();
            }
            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            {
                RuleFor(x => x.Phone).NotEmpty();
            }
            if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            {
                RuleFor(x => x.Fax).NotEmpty();
            }
        }
    }
}
