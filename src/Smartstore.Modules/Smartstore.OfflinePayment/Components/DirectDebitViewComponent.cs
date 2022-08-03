using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class DirectDebitViewComponent : OfflinePaymentViewComponentBase
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public DirectDebitViewComponent(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await GetPaymentInfoModelAsync<DirectDebitPaymentInfoModel, DirectDebitPaymentSettings>();
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;

            model.DirectDebitAccountHolder = (string)paymentData.Get("DirectDebitAccountHolder");
            model.DirectDebitAccountNumber = (string)paymentData.Get("DirectDebitAccountNumber");
            model.DirectDebitBankCode = (string)paymentData.Get("DirectDebitBankCode");
            model.DirectDebitBankName = (string)paymentData.Get("DirectDebitBankName");
            model.DirectDebitBic = (string)paymentData.Get("DirectDebitBic");
            model.DirectDebitCountry = (string)paymentData.Get("DirectDebitCountry");
            model.DirectDebitIban = (string)paymentData.Get("DirectDebitIban");

            return View(model);
        }
    }
}