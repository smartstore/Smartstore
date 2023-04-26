using FluentValidation;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Providers
{
    [SystemName("Payments.PayPalCreditCard")]
    [FriendlyName("PayPal Credit Card")]
    [Order(1)]
    public class PayPalCreditCardProvider : PayPalProviderBase
    {
        private readonly IValidator<PublicCreditCardModel> _validator;

        public PayPalCreditCardProvider(
            SmartDbContext db, 
            PayPalHttpClient client, 
            PayPalSettings settings,
            IValidator<PublicCreditCardModel> validator)
            : base(db, client, settings)
        {
            _validator = validator;
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalCreditCardViewComponent));

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        public override bool RequiresInteraction => true;

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new PublicCreditCardModel
            {
                CardholderName = form["CardholderName"],
                City = form["City"],
                Address1 = form["Address1"],
                Address2 = form["Address2"],
                ZipPostalCode = form["ZipPostalCode"],
                // TODO: (mh) (core)
                //CountryId = form["CountryId"],
                //StateProvinceId = form["StateProvinceId"]
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            // TODO: (mh) (core) Check which fields must be set here.
            var request = new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardCvv2 = form["CardCode"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardStartMonth = int.Parse(form["StartMonth"]),
                CreditCardStartYear = int.Parse(form["StartYear"]),
                CreditCardIssueNumber = form["IssueNumber"]
            };

            return Task.FromResult(request);
        }
    }
}