using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;
using Smartstore.OfflinePayment.Models;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class DirectDebitViewComponent : SmartViewComponent
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public DirectDebitViewComponent(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public IViewComponentResult Invoke()
        {
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;

            var model = new DirectDebitPaymentInfoModel
            {
                EnterIBAN = ((string)paymentData.Get("EnterIBAN")).NullEmpty() ?? "iban",
                DirectDebitAccountHolder = (string)paymentData.Get("DirectDebitAccountHolder"),
                DirectDebitAccountNumber = (string)paymentData.Get("DirectDebitAccountNumber"),
                DirectDebitBankCode = (string)paymentData.Get("DirectDebitBankCode"),
                DirectDebitBankName = (string)paymentData.Get("DirectDebitBankName"),
                DirectDebitBic = (string)paymentData.Get("DirectDebitBic"),
                DirectDebitCountry = (string)paymentData.Get("DirectDebitCountry"),
                DirectDebitIban = (string)paymentData.Get("DirectDebitIban")
            };

            return View(model);
        }
    }
}