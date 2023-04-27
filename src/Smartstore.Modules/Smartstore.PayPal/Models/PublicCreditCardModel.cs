using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.PayPal.Models
{
    public class PublicCreditCardModel : ModelBase
    {
        [Required]
        [LocalizedDisplay("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.StateProvince")]
        public string StateProvince { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.City")]
        public string City { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.Address1")]
        public string Address1 { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.Address2")]
        public string Address2 { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.Country")]
        public string Country { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.Country")]
        [UIHint("Countries")]
        public int? CountryId { get; set; }

        [Required]
        [LocalizedDisplay("Address.Fields.StateProvince")]
        public int? StateProvinceId { get; set; }
    }

    public partial class PayPalCreditCardValidator : SmartValidator<PublicCreditCardModel>
    {
        public PayPalCreditCardValidator(Localizer T)
        {
            // TODO: Proper validation.
            RuleFor(x => x.Country)
                .NotEmpty()
                .WithMessage(T("Plugins.Smartstore.PayPal.Country.NotEmpty"));
        }
    }
}