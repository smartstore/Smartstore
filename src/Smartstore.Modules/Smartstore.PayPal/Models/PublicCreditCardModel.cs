using FluentValidation;

namespace Smartstore.PayPal.Models
{
    [LocalizedDisplay("Address.Fields.")]
    public class PublicCreditCardModel : ModelBase
    {
        [LocalizedDisplay("Payment.CardholderName")]
        public string CardholderName { get; set; }

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
        }
    }
}