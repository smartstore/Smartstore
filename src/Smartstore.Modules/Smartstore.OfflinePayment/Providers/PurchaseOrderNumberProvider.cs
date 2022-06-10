using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.PurchaseOrderNumber")]
    [FriendlyName("Purchase Order Number")]
    [Order(1)]
    public class PurchaseOrderNumberProvider : OfflinePaymentProviderBase<PurchaseOrderNumberPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(PurchaseOrderNumberProvider);
        }

        // TODO: (mh) (core) Not needed here > make optional.
        protected override string GetProviderName()
        {
            return nameof(PurchaseOrderNumberProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("PurchaseOrderNumberConfigure", "OfflinePayment", new { area = "Admin" });

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
            var result = new ProcessPaymentResult();
            var settings = await CommonServices.SettingFactory.LoadSettingsAsync<PurchaseOrderNumberPaymentSettings>(processPaymentRequest.StoreId);

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
                    result.Errors.Add(T("Common.Payment.TranactionTypeNotSupported"));
                    return result;
            }

            return result;
        }

        public override bool RequiresInteraction => true;
    }
}