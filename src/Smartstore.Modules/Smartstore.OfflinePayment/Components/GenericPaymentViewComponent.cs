using Microsoft.AspNetCore.Mvc;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class GenericPaymentViewComponent : OfflinePaymentViewComponentBase
    {
        public override IViewComponentResult Invoke(string providerName)
        {
            switch (providerName)
            {
                case "CashOnDeliveryProvider":
                    return View(GetPaymentInfoModel<CashOnDeliveryPaymentInfoModel, CashOnDeliveryPaymentSettings>());
                case "InvoiceProvider":
                    return View(GetPaymentInfoModel<InvoicePaymentInfoModel, InvoicePaymentSettings>());
                case "PayInStoreProvider":
                    return View(GetPaymentInfoModel<PayInStorePaymentInfoModel, PayInStorePaymentSettings>());
                case "PrepaymentProvider":
                    return View(GetPaymentInfoModel<PrepaymentPaymentInfoModel, PrepaymentPaymentSettings>());
                default:
                    return Empty();
            }
        }
    }
}
