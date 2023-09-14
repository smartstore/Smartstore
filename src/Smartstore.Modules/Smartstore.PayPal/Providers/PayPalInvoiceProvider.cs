using FluentValidation;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Providers
{
    [SystemName(PayPalConstants.PayUponInvoice)]
    [FriendlyName("PayPal Pay Upon Invoice")]
    [Order(1)]
    public class PayPalInvoiceProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PayPalHttpClient _client;
        private readonly IValidator<PublicInvoiceModel> _validator;
        
        public PayPalInvoiceProvider(
            SmartDbContext db, 
            ICheckoutStateAccessor checkoutStateAccessor, 
            PayPalHttpClient client, 
            IValidator<PublicInvoiceModel> validator)
        {
            _db = db;
            _client = client;
            _checkoutStateAccessor = checkoutStateAccessor;
            _validator = validator;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "PayPal", new { area = "Admin" });

        /// <summary>
        /// Payments via invoice must be captured at once.
        /// </summary>
        public override bool SupportCapture => false;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        /// <summary>
        /// Payments via invoice must be captured at once. Only order with PaymentStatus.Authorized can be voided.
        /// </summary>
        public override bool SupportVoid => false;

        public override bool RequiresInteraction => true;

        public override RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayPalInvoiceViewComponent));

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new PublicInvoiceModel
            {
                DateOfBirth = DateTime.Parse(form["DateOfBirth"].ToString()),
                PhoneNumber = form["PhoneNumber"]
            };

            var result = await _validator.ValidateAsync(model);

            if (result.Errors.Count > 0)
            {
                if(!result.Errors.Any(x => x.PropertyName == nameof(PublicInvoiceModel.PhoneNumber)))
                {
                    _checkoutStateAccessor.CheckoutState.PaymentData["PayPalInvoicePhoneNumber"] = form["PhoneNumber"].ToString();
                }

                if (!result.Errors.Any(x => x.PropertyName == nameof(PublicInvoiceModel.DateOfBirth)))
                {
                    _checkoutStateAccessor.CheckoutState.PaymentData["PayPalInvoiceBirthdate"] = model.DateOfBirth.ToString("yyyy-MM-dd");
                }
            }

            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            _checkoutStateAccessor.CheckoutState.PaymentData["PayPalInvoiceBirthdate"] = DateTime.Parse(form["DateOfBirth"]).ToString("yyyy-MM-dd");
            _checkoutStateAccessor.CheckoutState.PaymentData["PayPalInvoicePhoneNumber"] = form["PhoneNumber"].ToString();

            var request = new ProcessPaymentRequest
            {
                OrderGuid = Guid.NewGuid()
            };

            return Task.FromResult(request);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            if (!_checkoutStateAccessor.CheckoutState.PaymentData.TryGetValueAs<string>("ClientMetaId", out var clientMetaId))
            {
                throw new PayPalException(T("Payment.MissingCheckoutState", "PayPalCheckoutState." + nameof(clientMetaId)));
            }

            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending,
            };

            var response = await _client.CreateOrderForInvoiceAsync(request);

            return result;
        }
        
        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request)
        {
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            var response = await _client.RefundPaymentAsync(request, result);
            var refund = response.Body<RefundMessage>();

            if (refund.Id.HasValue() && request.Order.Id != 0)
            {
                var refundIds = request.Order.GenericAttributes.Get<List<string>>("Payments.PayPalStandard.RefundId") ?? new List<string>();
                if (!refundIds.Contains(refund.Id))
                {
                    refundIds.Add(refund.Id);
                }

                request.Order.GenericAttributes.Set("Payments.PayPalStandard.RefundId", refundIds);
                await _db.SaveChangesAsync();

                result.NewPaymentStatus = request.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
            }

            return result;
        }

        // TODO: (mh) (core) Implement in future
        //public override async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request)
        //{
        //    var result = new ProcessPaymentResult
        //    {
        //        NewPaymentStatus = request.Order.PaymentStatus
        //    };

        //    return result;
        //}

        //public override Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request)
        //{
        //    throw new System.NotImplementedException();
        //}
    }
}
