using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
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
        private readonly SmartDbContext _db;
        private readonly IValidator<ManualPaymentInfoModel> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IEncryptor _encryptor;

        public ManualProvider(
            SmartDbContext db,
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<ManualPaymentInfoModel> validator,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IEncryptor encryptor)
            : base(storeContext, settingFactory)
        {
            _db = db;
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

        public override async Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart)
        {
            var lastOrder = await _db.Orders
                .Where(x => x.PaymentMethodSystemName == "Payments.Manual" && x.AllowStoringCreditCardNumber)
                .ApplyStandardFilter(cart.Customer.Id, cart.StoreId)
                .FirstOrDefaultAsync();
            if (lastOrder == null)
            {
                return null;
            }

            var model = new ManualPaymentInfoModel
            {
                CreditCardType = _encryptor.DecryptText(lastOrder.CardType),
                CardholderName = _encryptor.DecryptText(lastOrder.CardName),
                CardNumber = _encryptor.DecryptText(lastOrder.CardNumber),
                ExpireMonth = _encryptor.DecryptText(lastOrder.CardExpirationMonth),
                ExpireYear = _encryptor.DecryptText(lastOrder.CardExpirationYear),
                CardCode = _encryptor.DecryptText(lastOrder.CardCvv2)
            };

            var validation = await _validator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return null;
            }

            var request = new ProcessPaymentRequest
            {
                CreditCardType = model.CreditCardType,
                CreditCardName = model.CardholderName,
                CreditCardNumber = model.CardNumber,
                CreditCardExpireMonth = model.ExpireMonth.ToInt(),
                CreditCardExpireYear = model.ExpireYear.ToInt(),
                CreditCardCvv2 = model.CardCode
            };

            // State payment data required when navigating back to payment selection.
            var state = _checkoutStateAccessor.CheckoutState;
            state.PaymentData["CreditCardType"] = request.CreditCardType;
            state.PaymentData["CardholderName"] = request.CreditCardName;
            state.PaymentData["CardNumber"] = request.CreditCardNumber;
            state.PaymentData["ExpireMonth"] = model.ExpireMonth;
            state.PaymentData["ExpireYear"] = model.ExpireYear;
            state.PaymentData["CardCode"] = request.CreditCardCvv2;

            return request;
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = await base.ProcessPaymentAsync(processPaymentRequest);
            result.AllowStoringCreditCardNumber = true;

            return result;
        }

        public override async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = await base.ProcessPaymentAsync(processPaymentRequest);
            result.AllowStoringCreditCardNumber = true;

            return result;
        }

        public override Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult());
        }
    }
}