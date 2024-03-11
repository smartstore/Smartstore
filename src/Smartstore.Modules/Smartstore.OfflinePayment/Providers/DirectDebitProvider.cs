using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
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
        private readonly IEncryptor _encryptor;

        public DirectDebitProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<DirectDebitPaymentInfoModel> validator,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IEncryptor encryptor)
            : base(storeContext, settingFactory)
        {
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
            _encryptor = encryptor;
        }

        public override bool RequiresInteraction => true;

        protected override Type GetViewComponentType()
            => typeof(DirectDebitViewComponent);

        public RouteInfo GetConfigurationRoute()
            => new("DirectDebitConfigure", "OfflinePayment", new { area = "Admin" });

        public override Task<string> GetPaymentSummaryAsync()
        {
            var result = string.Empty;

            if (_httpContextAccessor.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var pr) && pr != null)
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
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitCountry = form["DirectDebitCountry"],
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
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitCountry = form["DirectDebitCountry"],
                DirectDebitIban = form["DirectDebitIban"],
                DirectDebitBic = form["DirectDebitBic"]
            };

            return Task.FromResult(paymentInfo);
        }

        public override Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart, Order lastOrder)
        {
            if (!lastOrder.AllowStoringDirectDebit)
            {
                return null;
            }

            var request = new ProcessPaymentRequest
            {
                DirectDebitAccountHolder = _encryptor.DecryptText(lastOrder.DirectDebitAccountHolder),
                DirectDebitAccountNumber = _encryptor.DecryptText(lastOrder.DirectDebitAccountNumber),
                DirectDebitBankCode = _encryptor.DecryptText(lastOrder.DirectDebitBankCode),
                DirectDebitBankName = _encryptor.DecryptText(lastOrder.DirectDebitBankName),
                DirectDebitCountry = _encryptor.DecryptText(lastOrder.DirectDebitCountry),
                DirectDebitIban = _encryptor.DecryptText(lastOrder.DirectDebitIban),
                DirectDebitBic = _encryptor.DecryptText(lastOrder.DirectDebitBIC)
            };

            var state = _checkoutStateAccessor.CheckoutState;
            var enterIban = request.DirectDebitIban.HasValue() ? "iban" : "no-iban";

            // Required for payment summary.
            state.CustomProperties["Payments.DirectDebit.EnterIban"] = enterIban;

            // Required when navigating back to payment selection.
            state.PaymentData["EnterIBAN"] = enterIban;
            state.PaymentData["DirectDebitAccountHolder"] = request.DirectDebitAccountHolder;
            state.PaymentData["DirectDebitAccountNumber"] = request.DirectDebitAccountNumber;
            state.PaymentData["DirectDebitBankCode"] = request.DirectDebitBankCode;
            state.PaymentData["DirectDebitBankName"] = request.DirectDebitBankName;
            state.PaymentData["DirectDebitCountry"] = request.DirectDebitCountry;
            state.PaymentData["DirectDebitIban"] = request.DirectDebitIban;
            state.PaymentData["DirectDebitBic"] = request.DirectDebitBic;

            return Task.FromResult(request);
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
    }
}