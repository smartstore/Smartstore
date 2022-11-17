using System.ComponentModel.DataAnnotations;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Common
{
    [LocalizedDisplay("Address.Fields.")]
    public partial class AddressModel : EntityModelBase
    {
        [LocalizedDisplay("*Salutation")]
        public string Salutation { get; set; }
        public bool SalutationEnabled { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
        public bool TitleEnabled { get; set; }

        [Required]
        [LocalizedDisplay("*FirstName")]
        public string FirstName { get; set; }
        public bool FirstNameEnabled { get; set; } = true;

        [Required]
        [LocalizedDisplay("*LastName")]
        public string LastName { get; set; }
        public bool LastNameEnabled { get; set; } = true;

        [Required]
        [LocalizedDisplay("*Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        public bool EmailEnabled { get; set; } = true;

        [LocalizedDisplay("*EmailMatch")]
        [DataType(DataType.EmailAddress)]
        public string EmailMatch { get; set; }
        public bool ValidateEmailAddress { get; set; }

        [LocalizedDisplay("*Company")]
        public string Company { get; set; }

        public bool CompanyEnabled { get; set; }
        public bool CompanyRequired { get; set; }

        [LocalizedDisplay("*Country")]
        public int? CountryId { get; set; }

        [LocalizedDisplay("*Country")]
        public string CountryName { get; set; }
        public bool CountryEnabled { get; set; }
        public bool CountryRequired { get; set; }

        [LocalizedDisplay("*StateProvince")]
        public int? StateProvinceId { get; set; }
        public bool StateProvinceEnabled { get; set; }
        public bool StateProvinceRequired { get; set; }

        [LocalizedDisplay("*StateProvince")]
        public string StateProvinceName { get; set; }

        [LocalizedDisplay("*City")]
        public string City { get; set; }
        public bool CityEnabled { get; set; }
        public bool CityRequired { get; set; }

        [LocalizedDisplay("*Address1")]
        public string Address1 { get; set; }
        public bool StreetAddressEnabled { get; set; }
        public bool StreetAddressRequired { get; set; }

        [LocalizedDisplay("*Address2")]
        public string Address2 { get; set; }
        public bool StreetAddress2Enabled { get; set; }
        public bool StreetAddress2Required { get; set; }

        [LocalizedDisplay("*ZipPostalCode")]
        public string ZipPostalCode { get; set; }
        public bool ZipPostalCodeEnabled { get; set; }
        public bool ZipPostalCodeRequired { get; set; }

        [LocalizedDisplay("*PhoneNumber")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
        public bool PhoneEnabled { get; set; }
        public bool PhoneRequired { get; set; }

        [LocalizedDisplay("*FaxNumber")]
        [DataType(DataType.PhoneNumber)]
        public string FaxNumber { get; set; }
        public bool FaxEnabled { get; set; }
        public bool FaxRequired { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableSalutations { get; set; } = new List<SelectListItem>();

        public string FormattedAddress { get; set; }

        public string GetFormattedName()
        {
            if (LastName.IsEmpty())
            {
                return FirstName.EmptyNull();
            }
            else
            {
                return FirstName.RightPad() + LastName;
            }
        }

        public string GetFormattedCityStateZip()
        {
            var sb = new StringBuilder(50);

            if (CityEnabled && City.HasValue())
            {
                sb.Append(City);
                if ((StateProvinceEnabled && StateProvinceName.HasValue()) || (ZipPostalCodeEnabled && ZipPostalCode.HasValue()))
                {
                    sb.Append(", ");
                }
            }

            if (StateProvinceEnabled && StateProvinceName.HasValue())
            {
                sb.Append(StateProvinceName);
                if (ZipPostalCodeEnabled && ZipPostalCode.HasValue())
                {
                    sb.Append(' ');
                }
            }

            if (ZipPostalCodeEnabled && ZipPostalCode.HasValue())
            {
                sb.Append(ZipPostalCode);
            }

            return sb.ToString();
        }
    }

    public class AddressValidator : SmartValidator<AddressModel>
    {
        public AddressValidator(Localizer T, AddressSettings addressSettings, SmartDbContext db)
        {
            if (addressSettings.CountryEnabled)
            {
                if (addressSettings.CountryRequired)
                {
                    RuleFor(x => x.CountryId)
                        .NotNull()
                        .NotEqual(0)
                        .WithMessage(T("Admin.Address.Fields.Country.Required"));
                }

                RuleFor(x => x.CountryId)
                    .Must(id => id == 0 || db.Countries.Any(x => x.Id == id && x.Published))
                    .WithMessage(T("Admin.Address.Fields.Country.MustBePublished"));
            }

            if (addressSettings.StateProvinceRequired && addressSettings.StateProvinceEnabled)
            {
                RuleFor(x => x.StateProvinceId).NotNull().NotEqual(0);
            }

            if (addressSettings.CompanyRequired && addressSettings.CompanyEnabled)
            {
                RuleFor(x => x.Company).NotEmpty();
            }

            if (addressSettings.StreetAddressRequired && addressSettings.StreetAddressEnabled)
            {
                RuleFor(x => x.Address1).NotEmpty();
            }

            if (addressSettings.StreetAddress2Required && addressSettings.StreetAddress2Enabled)
            {
                RuleFor(x => x.Address2).NotEmpty();
            }

            if (addressSettings.ZipPostalCodeRequired && addressSettings.ZipPostalCodeEnabled)
            {
                RuleFor(x => x.ZipPostalCode).NotEmpty();
            }

            if (addressSettings.CityRequired && addressSettings.CityEnabled)
            {
                RuleFor(x => x.City).NotEmpty();
            }

            if (addressSettings.PhoneRequired && addressSettings.PhoneEnabled)
            {
                RuleFor(x => x.PhoneNumber).NotEmpty();
            }

            if (addressSettings.FaxRequired && addressSettings.FaxEnabled)
            {
                RuleFor(x => x.FaxNumber).NotEmpty();
            }

            RuleFor(x => x.Email).EmailAddress();

            if (addressSettings.ValidateEmailAddress)
            {
                RuleFor(x => x.EmailMatch)
                    .NotEmpty()
                    .EmailAddress()
                    .Equal(x => x.Email)
                    .WithMessage(T("Admin.Address.Fields.EmailMatch.MustMatchEmail"));
            }
        }
    }
}
