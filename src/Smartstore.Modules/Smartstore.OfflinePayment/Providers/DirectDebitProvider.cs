using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.DirectDebit")]
    [FriendlyName("Direct Debit")]
    [Order(1)]
    public class DirectDebitProvider : OfflinePaymentProviderBase<DirectDebitPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(DirectDebitViewComponent);
        }

        // TODO: (not needed here make optional)
        protected override string GetProviderName()
        {
            return nameof(DirectDebitProvider);
        }
        public RouteInfo GetConfigurationRoute()
            => new("DirectDebitConfigure", "OfflinePayment", new { area = "Admin" });

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