using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.PayInStore")]
    [FriendlyName("Pay In Store")]
    [Order(100)]
    public class PayInStoreProvider : OfflinePaymentProviderBase<PayInStorePaymentSettings>, IConfigurable
    {
        public PayInStoreProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory)
            : base(storeContext, settingFactory)
        {
        }

        public RouteInfo GetConfigurationRoute()
            => new("PayInStoreConfigure", "OfflinePayment", new { area = "Admin" });
    }
}