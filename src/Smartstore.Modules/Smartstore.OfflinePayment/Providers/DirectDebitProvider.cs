using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.DirectDebit")]
    [FriendlyName("Direct Debit")]
    [Order(100)]
    public class DirectDebitProvider : OfflinePaymentProviderBase<DirectDebitPaymentSettings>, IConfigurable
    {
        private readonly IValidator<DirectDebitPaymentInfoModel> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public DirectDebitProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<DirectDebitPaymentInfoModel> validator,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor)
            : base(storeContext, settingFactory)
        {
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        protected override Type GetViewComponentType()
            => typeof(DirectDebitViewComponent);

        public RouteInfo GetConfigurationRoute()
            => new("DirectDebitConfigure", "OfflinePayment", new { area = "Admin" });

        public override Task<string> GetPaymentSummaryAsync()
        {
            var result = string.Empty;

            if (_httpContextAccessor.HttpContext.Session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var pr) && pr != null)
            {
                var enterIban = _checkoutStateAccessor.CheckoutState.CustomProperties.Get("Payments.DirectDebit.EnterIban") as string;
                if (enterIban.EqualsNoCase("iban"))
                {
                    result = $"{pr.DirectDebitBic}, {pr.DirectDebitIban.Mask(8)}";
                }
                else
                {
                    result = $"{pr.DirectDebitBankCode}, {pr.DirectDebitAccountNumber.Mask(4)}";
                }
            }

            return Task.FromResult(result);
        }

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new DirectDebitPaymentInfoModel
            {
                EnterIBAN = form["EnterIBAN"],
                DirectDebitAccountHolder = form["DirectDebitAccountHolder"],
                DirectDebitAccountNumber = form["DirectDebitAccountNumber"],
                DirectDebitBankCode = form["DirectDebitBankCode"],
                DirectDebitCountry = form["DirectDebitCountry"],
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitIban = form["DirectDebitIban"],
                DirectDebitBic = form["DirectDebitBic"]
            };

            _checkoutStateAccessor.CheckoutState.CustomProperties["Payments.DirectDebit.EnterIban"] = model.EnterIBAN;

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                DirectDebitAccountHolder = form["DirectDebitAccountHolder"],
                DirectDebitAccountNumber = form["DirectDebitAccountNumber"],
                DirectDebitBankCode = form["DirectDebitBankCode"],
                DirectDebitCountry = form["DirectDebitCountry"],
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitIban = form["DirectDebitIban"],
                DirectDebitBic = form["DirectDebitBic"]
            };

            return Task.FromResult(paymentInfo);
        }

        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                AllowStoringDirectDebit = true,
                NewPaymentStatus = PaymentStatus.Pending
            };

            return Task.FromResult(result);
        }

        public override bool RequiresInteraction => true;
    }
}