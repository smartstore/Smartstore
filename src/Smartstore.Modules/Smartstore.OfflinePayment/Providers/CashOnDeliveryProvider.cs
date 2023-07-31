using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.CashOnDelivery")]
    [FriendlyName("Cash On Delivery (COD)")]
    [Order(100)]
    public class CashOnDeliveryProvider : OfflinePaymentProviderBase<CashOnDeliveryPaymentSettings>, IConfigurable
    {
        public CashOnDeliveryProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory)
            : base(storeContext, settingFactory)
        {
        }

        public RouteInfo GetConfigurationRoute()
            => new("CashOnDeliveryConfigure", "OfflinePayment", new { area = "Admin" });
    }
}