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