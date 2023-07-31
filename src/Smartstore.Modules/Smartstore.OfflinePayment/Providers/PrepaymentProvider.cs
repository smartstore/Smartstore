using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.Prepayment")]
    [FriendlyName("Prepayment")]
    [Order(100)]
    public class PrepaymentProvider : OfflinePaymentProviderBase<PrepaymentPaymentSettings>, IConfigurable
    {
        public PrepaymentProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory)
            : base(storeContext, settingFactory)
        {
        }

        public RouteInfo GetConfigurationRoute()
            => new("PrepaymentConfigure", "OfflinePayment", new { area = "Admin" });
    }
}