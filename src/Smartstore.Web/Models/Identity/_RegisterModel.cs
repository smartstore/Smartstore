using System;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Validation;

namespace Smartstore.Web.Models.Identity
{
    [LocalizedDisplay("Account.Fields.")]
    public partial class RegisterModel : ModelBase
    {
        public bool UserNamesEnabled { get; set; }

        [LocalizedDisplay("Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [LocalizedDisplay("*Username")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*ConfirmPassword")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterModelValidator : SmartValidator<RegisterModel>
    {
        public RegisterModelValidator(Localizer T, CustomerSettings customerSettings, TaxSettings taxSettings, SmartDbContext db)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password).WithMessage(T("Account.Fields.Password.EnteredPasswordsDoNotMatch"));
            
            //// Form fields.
            //if (customerSettings.FirstNameRequired)
            //{
            //    RuleFor(x => x.FirstName).NotEmpty();
            //}
            //if (customerSettings.LastNameRequired)
            //{
            //    RuleFor(x => x.LastName).NotEmpty();
            //}
            //if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            //{
            //    RuleFor(x => x.Company).NotEmpty();
            //}
            //if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            //{
            //    RuleFor(x => x.StreetAddress).NotEmpty();
            //}
            //if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            //{
            //    RuleFor(x => x.StreetAddress2).NotEmpty();
            //}
            //if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            //{
            //    RuleFor(x => x.ZipPostalCode).NotEmpty();
            //}
            //if (customerSettings.CityRequired && customerSettings.CityEnabled)
            //{
            //    RuleFor(x => x.City).NotEmpty();
            //}
            //if (customerSettings.StateProvinceRequired && customerSettings.StateProvinceEnabled && customerSettings.CountryEnabled)
            //{
            //    RuleFor(x => x.StateProvinceId)
            //        .NotNull()
            //        .NotEqual(0)
            //        .WithMessage(T("Address.Fields.StateProvince.Required"));
            //}
            //if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            //{
            //    RuleFor(x => x.Phone).NotEmpty();
            //}
            //if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            //{
            //    RuleFor(x => x.Fax).NotEmpty();
            //}
            //if (taxSettings.EuVatEnabled && taxSettings.VatRequired)
            //{
            //    RuleFor(x => x.VatNumber).NotEmpty();
            //}
        }
    }
}
