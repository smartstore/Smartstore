using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.Invoice")]
    [FriendlyName("Invoice")]
    [Order(100)]
    public class InvoiceProvider : OfflinePaymentProviderBase<InvoicePaymentSettings>, IConfigurable
    {
        public InvoiceProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory)
            : base(storeContext, settingFactory)
        {
        }

        public RouteInfo GetConfigurationRoute()
            => new("InvoiceConfigure", "OfflinePayment", new { area = "Admin" });
    }
}