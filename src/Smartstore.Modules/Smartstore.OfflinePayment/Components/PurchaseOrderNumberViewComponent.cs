using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class PurchaseOrderNumberViewComponent : OfflinePaymentViewComponentBase
    {
        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await GetPaymentInfoModelAsync<PurchaseOrderNumberPaymentInfoModel, PurchaseOrderNumberPaymentSettings>();

            var paymentData = HttpContext.GetCheckoutState().PaymentData;

            model.PurchaseOrderNumber = (string)paymentData.Get("PurchaseOrderNumber");

            return View(model);
        }
    }
}
