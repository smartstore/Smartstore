using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.PayInStore")]
    [FriendlyName("Pay In Store")]
    [Order(1)]
    public class PayInStoreProvider : OfflinePaymentProviderBase<PayInStorePaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(GenericPaymentViewComponent);
        }

        protected override string GetProviderName()
        {
            return nameof(PayInStoreProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("PayInStoreConfigure", "OfflinePayment", new { area = "Admin" });
    }
}