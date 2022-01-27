using Microsoft.AspNetCore.Mvc;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class GenericPaymentViewComponent : OfflinePaymentViewComponentBase
    {
        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            PaymentInfoModelBase model = null;

            switch (providerName)
            {
                case "CashOnDeliveryProvider":
                    model = await GetPaymentInfoModelAsync<CashOnDeliveryPaymentInfoModel, CashOnDeliveryPaymentSettings>();
                    break;
                case "InvoiceProvider":
                    model = await GetPaymentInfoModelAsync<InvoicePaymentInfoModel, InvoicePaymentSettings>();
                    break;
                case "PayInStoreProvider":
                    model = await GetPaymentInfoModelAsync<PayInStorePaymentInfoModel, PayInStorePaymentSettings>();
                    break;
                case "PrepaymentProvider":
                    model = await GetPaymentInfoModelAsync<PrepaymentPaymentInfoModel, PrepaymentPaymentSettings>();
                    break;
            }

            if (model != null)
            {
                return View(model);
            }

            return Empty();
        }
    }
}
