using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.CashOnDelivery")]
    [FriendlyName("Cash On Delivery (COD)")]
    [Order(1)]
    public class CashOnDeliveryProvider : OfflinePaymentProviderBase<CashOnDeliveryPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(GenericPaymentViewComponent);
        }

        protected override string GetProviderName()
        {
            return nameof(CashOnDeliveryProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("CashOnDeliveryConfigure", "OfflinePayment", new { area = "Admin" });
    }
}