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
    [SystemName(PayPalConstants.CreditCard)]
    [FriendlyName("PayPal Credit Card")]
    [Order(1)]
    public class PayPalCreditCardProvider : PayPalProviderBase
    {
        private readonly IValidator<PublicCreditCardModel> _validator;

        public PayPalCreditCardProvider(
            SmartDbContext db, 
            PayPalHttpClient client, 
            PayPalSettings settings,
            IPaymentService paymentService,
            ICheckoutStateAccessor checkoutStateAccessor,
            IValidator<PublicCreditCardModel> validator)
            : base(db, client, settings, paymentService, checkoutStateAccessor)
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
                CountryId = int.Parse(form["CountryId"]),
                StateProvinceId = int.Parse(form["StateProvinceId"])
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var request = new ProcessPaymentRequest
            {
                OrderGuid = Guid.NewGuid()
            };

            return Task.FromResult(request);
        }
    }
}