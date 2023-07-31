using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Orders;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class ManualPaymentViewComponent : SmartViewComponent
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ManualPaymentSettings _manualPaymentSettings;

        public ManualPaymentViewComponent(
            ICheckoutStateAccessor checkoutStateAccessor,
            ManualPaymentSettings manualPaymentSettings)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
            _manualPaymentSettings = manualPaymentSettings;
        }

        public IViewComponentResult Invoke()
        {
            var excludedCreditCards = _manualPaymentSettings.ExcludedCreditCards.SplitSafe(',');
            var model = new ManualPaymentInfoModel();

            foreach (var pair in ManualProvider.GetCreditCardBrands(T))
            {
                if (!excludedCreditCards.Any(x => x.EqualsNoCase(pair.Key)))
                {
                    model.CreditCardTypes.Add(new()
                    {
                        Text = pair.Value,
                        Value = pair.Key
                    });
                }
            }

            // Years.
            for (var i = 0; i < 15; i++)
            {
                var year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem { Text = year, Value = year });
            }

            // Months.
            for (var i = 1; i <= 12; i++)
            {
                var text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem { Text = text, Value = i.ToString() });
            }

            // Set postback values.
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;
            model.CardholderName = (string)paymentData.Get("CardholderName");
            model.CardNumber = (string)paymentData.Get("CardNumber");
            model.CardCode = (string)paymentData.Get("CardCode");

            var creditCardType = (string)paymentData.Get("CreditCardType");
            var selectedCcType = model.CreditCardTypes.Where(x => x.Value.EqualsNoCase(creditCardType)).FirstOrDefault();
            if (selectedCcType != null)
            {
                selectedCcType.Selected = true;
            }

            var expireMonth = (string)paymentData.Get("ExpireMonth");
            var selectedMonth = model.ExpireMonths.Where(x => x.Value.EqualsNoCase(expireMonth)).FirstOrDefault();
            if (selectedMonth != null)
            {
                selectedMonth.Selected = true;
            }

            var expireYear = (string)paymentData.Get("ExpireYear");
            var selectedYear = model.ExpireYears.Where(x => x.Value.EqualsNoCase(expireYear)).FirstOrDefault();
            if (selectedYear != null)
            {
                selectedYear.Selected = true;
            }

            return View(model);
        }
    }
}
