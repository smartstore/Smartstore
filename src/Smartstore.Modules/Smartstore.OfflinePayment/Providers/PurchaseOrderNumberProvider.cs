using Microsoft.AspNetCore.Http;
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
    [SystemName("Payments.PurchaseOrderNumber")]
    [FriendlyName("Purchase Order Number")]
    [Order(100)]
    public class PurchaseOrderNumberProvider : OfflinePaymentProviderBase<PurchaseOrderNumberPaymentSettings>, IConfigurable
    {
        private readonly IValidator<PurchaseOrderNumberPaymentInfoModel> _validator;

        public PurchaseOrderNumberProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<PurchaseOrderNumberPaymentInfoModel> validator)
            : base(storeContext, settingFactory)
        {
            _validator = validator;
        }

        protected override Type GetViewComponentType()
            => typeof(PurchaseOrderNumberViewComponent);

        public RouteInfo GetConfigurationRoute()
            => new("PurchaseOrderNumberConfigure", "OfflinePayment", new { area = "Admin" });

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new PurchaseOrderNumberPaymentInfoModel
            {
                PurchaseOrderNumber = form["PurchaseOrderNumber"]
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                PurchaseOrderNumber = form["PurchaseOrderNumber"]
            };

            return Task.FromResult(paymentInfo);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var settings = await _settingFactory.LoadSettingsAsync<PurchaseOrderNumberPaymentSettings>(processPaymentRequest.StoreId);
            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = true
            };

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
                    result.Errors.Add(T("Common.Payment.TranactionTypeNotSupported"));
                    return result;
            }

            return result;
        }

        public override bool RequiresInteraction => true;
    }
}