using FluentValidation;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Providers
{
    public abstract class PayPalApmProviderBase : PayPalProviderBase
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IValidator<PublicApmModel> _validator;

        public PayPalApmProviderBase(PayPalApmServiceContext context)
            : base(context.Db, context.Client, context.Settings, context.PaymentService, context.CheckoutStateAccessor)
        {
            _db = context.Db;
            _checkoutStateAccessor = context.CheckoutStateAccessor;
            _validator = context.Validator;
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalApmViewComponent));

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        public override bool RequiresInteraction => true;

        public override bool SupportCapture => false;

        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

            if (!state.PayPalOrderId.HasValue())
            {
                throw new PayPalException(T("Payment.MissingCheckoutState", "PayPalCheckoutState." + nameof(request.PayPalOrderId)));
            }

            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending,
            };

            return Task.FromResult(result);
        }

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var countryId = Convert.ToInt32(form["CountryId"]);
            var country = await _db.Countries.FindByIdAsync(countryId, false);

            var model = new PublicApmModel
            {
                FullName = form["FullName"],
                CountryCode = country.TwoLetterIsoCode,
                BIC = form["BIC"],
                Email = form["Email"],
                Funding = form["Funding"]
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override async Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {   
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

            // Add Fullname & CountryCode to checkout state.
            var countryId = Convert.ToInt32(form["CountryId"]);
            var country = await _db.Countries.FindByIdAsync(countryId, false);

            state.ApmFullname = form["FullName"];
            state.ApmCountryCode = country.TwoLetterIsoCode;
            state.ApmProviderSystemName = form["paymentmethod"];

            // Add Email & BIC to checkout state if available.
            state.ApmEmail = form["Email"].ToString().HasValue() ? form["Email"] : string.Empty;
            state.ApmBIC = form["BIC"].ToString().HasValue() ? form["BIC"] : string.Empty;

            // INFO: OrderGuid is stored in checkout state, otherwise the transaction data hash may not match.
            // When going back in checkout, the user would be redirected to PayPal again, even though his payment details have not changed at all.
            var request = new ProcessPaymentRequest
            {
                OrderGuid = Guid.NewGuid()
            };

            return request;
        }
    }
}