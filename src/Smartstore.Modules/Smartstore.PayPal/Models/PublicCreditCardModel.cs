using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.PayPal.Models
{
    public class PublicCreditCardModel : ModelBase
    {
        [LocalizedDisplay("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [LocalizedDisplay("Address.Fields.City")]
        public string City { get; set; }

        [LocalizedDisplay("Address.Fields.Address1")]
        public string Address1 { get; set; }

        [LocalizedDisplay("Address.Fields.Address2")]
        public string Address2 { get; set; }

        [LocalizedDisplay("Address.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        [LocalizedDisplay("Address.Fields.Country")]
        public string Country { get; set; }

        [LocalizedDisplay("Address.Fields.Country")]
        [UIHint("Countries")]
        public int? CountryId { get; set; }

        [LocalizedDisplay("Address.Fields.StateProvince")]
        public int? StateProvinceId { get; set; }

        [LocalizedDisplay("Address.Fields.StateProvince")]
        public string StateProvince { get; set; }

        /// <summary>
        /// A value indicating whether the client token was retrieved from the PayPal API.
        /// </summary>
        public bool HasClientToken { get; set; }
    }

    public partial class PayPalCreditCardValidator : SmartValidator<PublicCreditCardModel>
    {
        public PayPalCreditCardValidator(Localizer T)
        {
            RuleFor(x => x.CardholderName).NotEmpty();
            RuleFor(x => x.City).NotEmpty();
            RuleFor(x => x.Address1).NotEmpty();
            RuleFor(x => x.Address2).NotEmpty();
            RuleFor(x => x.ZipPostalCode).NotEmpty();

            RuleFor(x => x.StateProvinceId)
                .NotEmpty()
                .WithMessage(T("Plugins.Smartstore.PayPal.StateProvince.NotEmpty"));

            RuleFor(x => x.CountryId)
                .NotEmpty()
                .WithMessage(T("Plugins.Smartstore.PayPal.Country.NotEmpty"));
        }
    }
}