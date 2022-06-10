using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.Invoice")]
    [FriendlyName("Invoice")]
    [Order(1)]
    public class InvoiceProvider : OfflinePaymentProviderBase<InvoicePaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(GenericPaymentViewComponent);
        }

        protected override string GetProviderName()
        {
            return nameof(InvoiceProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("InvoiceConfigure", "OfflinePayment", new { area = "Admin" });
    }
}