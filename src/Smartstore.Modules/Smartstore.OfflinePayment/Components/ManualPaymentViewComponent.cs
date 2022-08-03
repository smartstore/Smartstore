using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Orders;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class ManualPaymentViewComponent : OfflinePaymentViewComponentBase
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public ManualPaymentViewComponent(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await GetPaymentInfoModelAsync<ManualPaymentInfoModel, ManualPaymentSettings>((m, s) =>
            {
                var excludedCreditCards = s.ExcludedCreditCards.SplitSafe(',');

                foreach (var creditCard in ManualProvider.CreditCardTypes)
                {
                    if (!excludedCreditCards.Any(x => x.EqualsNoCase(creditCard.Value)))
                    {
                        m.CreditCardTypes.Add(new SelectListItem
                        {
                            Text = creditCard.Text,
                            Value = creditCard.Value
                        });
                    }
                }
            });

            // years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem { Text = year, Value = year });
            }

            // months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem { Text = text, Value = i.ToString() });
            }

            // set postback values
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
