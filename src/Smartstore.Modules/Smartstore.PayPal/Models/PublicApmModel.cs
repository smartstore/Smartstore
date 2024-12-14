using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.PayPal.Models
{
    public class PublicApmModel : ModelBase
    {
        public string Funding { get; set; }

        //[Required]
        [LocalizedDisplay("Plugins.Smartstore.PayPal.FullName")]
        public string FullName { get; set; }

        //[Required]
        [LocalizedDisplay("Address.Fields.Country")]
        [UIHint("Countries")]
        public int? CountryId { get; set; }

        public string CountryCode { get; set; }

        // EMail is needed for BLIK & Przelewy24
        [LocalizedDisplay("Address.Fields.Email")]
        public string Email { get; set; }

        // BIC is needed for iDEAL
        [LocalizedDisplay("Plugins.Smartstore.PayPal.BIC")]
        public string BIC { get; set; }
    }

    public partial class PayPalApmValidator : SmartValidator<PublicApmModel>
    {
        public PayPalApmValidator(Localizer T)
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(T("Plugins.Smartstore.PayPal.FullName.NotEmpty"));

            RuleFor(x => x.BIC)
                .Matches(RegularExpressions.IsBic)
                .When(x => x.Funding == "ideal");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddressStrict()
                .When(x => x.Funding == "p24" || x.Funding == "blik");

            RuleFor(x => x.CountryId)
                .GreaterThan(0)
                .WithMessage(T("Plugins.Smartstore.PayPal.Country.NotEmpty"));
        }
    }
}