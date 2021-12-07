using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.Prepayment")]
    [FriendlyName("Prepayment")]
    [Order(1)]
    public class PrepaymentProvider : OfflinePaymentProviderBase<PrepaymentPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(GenericPaymentViewComponent);
        }

        protected override string GetProviderName()
        {
            return nameof(PrepaymentProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("PrepaymentConfigure", "OfflinePayment", new { area = "Admin" });
    }
}