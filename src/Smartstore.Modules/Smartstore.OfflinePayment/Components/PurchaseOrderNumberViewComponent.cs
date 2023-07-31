using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;
using Smartstore.OfflinePayment.Models;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class PurchaseOrderNumberViewComponent : SmartViewComponent
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public PurchaseOrderNumberViewComponent(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public IViewComponentResult Invoke()
        {
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;
            var model = new PurchaseOrderNumberPaymentInfoModel
            {
                PurchaseOrderNumber = (string)paymentData.Get("PurchaseOrderNumber")
            };

            return View(model);
        }
    }
}
