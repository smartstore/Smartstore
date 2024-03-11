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
    [SystemName("Payments.Manual")]
    [FriendlyName("Credit Card (manual)")]
    [Order(100)]
    public class ManualProvider : OfflinePaymentProviderBase<ManualPaymentSettings>, IConfigurable
    {
        private readonly IValidator<ManualPaymentInfoModel> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IEncryptor _encryptor;

        public ManualProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<ManualPaymentInfoModel> validator,
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

        internal static Dictionary<string, string> GetCreditCardBrands(Localizer T)
        {
            var result = new Dictionary<string, string>();
            var brands = T("Plugins.Payments.Manual.CreditCardBrands").Value.SplitSafe('|');

            foreach (var str in brands)
            {
                if (str.SplitToPair(out var key, out var value, ";") && value.HasValue())
                {
                    result[key] = value;
                }
                else
                {
                    result[str] = str;
                }
            }

            return result;
        }

        public override bool RequiresInteraction => true;

        public override RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Manual;

        protected override Type GetViewComponentType()
            => typeof(ManualPaymentViewComponent);

        public RouteInfo GetConfigurationRoute()
            => new("ManualConfigure", "OfflinePayment", new { area = "Admin" });

        public override Task<string> GetPaymentSummaryAsync()
        {
            var result = string.Empty;

            if (_httpContextAccessor.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var pr) && pr != null)
            {
                var brandName = GetCreditCardBrands(T).Get(pr.CreditCardType.EmptyNull());

                result = $"{brandName}, {pr.CreditCardNumber.Mask(4)}";
            }

            return Task.FromResult(result);
        }

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new ManualPaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"]
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"].ToString().SplitSafe(',').FirstOrDefault()),
                CreditCardExpireYear = int.Parse(form["ExpireYear"].ToString().SplitSafe(',').FirstOrDefault()),
                CreditCardCvv2 = form["CardCode"]
            };

            return Task.FromResult(paymentInfo);
        }

        public override Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart, Order lastOrder)
        {
            if (!lastOrder.AllowStoringCreditCardNumber)
            {
                return null;
            }

            var request = new ProcessPaymentRequest
            {
                CreditCardType = _encryptor.DecryptText(lastOrder.CardType),
                CreditCardName = _encryptor.DecryptText(lastOrder.CardName),
                CreditCardNumber = _encryptor.DecryptText(lastOrder.CardNumber),
                CreditCardExpireMonth = _encryptor.DecryptText(lastOrder.CardExpirationMonth).ToInt(),
                CreditCardExpireYear = _encryptor.DecryptText(lastOrder.CardExpirationYear).ToInt(),
                CreditCardCvv2 = _encryptor.DecryptText(lastOrder.CardCvv2)
            };

            // Required when navigating back to payment selection.
            var state = _checkoutStateAccessor.CheckoutState;
            state.PaymentData["CreditCardType"] = request.CreditCardType;
            state.PaymentData["CardholderName"] = request.CreditCardName;
            state.PaymentData["CardNumber"] = request.CreditCardNumber;
            state.PaymentData["ExpireMonth"] = request.CreditCardStartMonth.ToStringInvariant();
            state.PaymentData["ExpireYear"] = request.CreditCardStartYear.ToStringInvariant();
            state.PaymentData["CardCode"] = request.CreditCardCvv2;

            return Task.FromResult(request);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return await GetProcessPaymentResultAsync(processPaymentRequest);
        }

        public override async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return await GetProcessPaymentResultAsync(processPaymentRequest);
        }

        public override Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        private async Task<ProcessPaymentResult> GetProcessPaymentResultAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var settings = await _settingFactory.LoadSettingsAsync<ManualPaymentSettings>(processPaymentRequest.StoreId);

            result.AllowStoringCreditCardNumber = true;

            switch (settings.TransactMode)
            {
                case TransactMode.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case TransactMode.Authorize:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case TransactMode.Paid:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                default:
                    throw new PaymentException(T("Common.Payment.TranactionTypeNotSupported"));
            }

            return result;
        }
    }
}