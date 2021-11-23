using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class ManualPaymentViewComponent : OfflinePaymentViewComponentBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ManualPaymentViewComponent(
            IComponentContext ctx,
            IMediaService mediaService,
            IHttpContextAccessor httpContextAccessor) : base(ctx, mediaService)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await PaymentInfoGetAsync<ManualPaymentInfoModel, ManualPaymentSettings>((m, s) =>
            {
                var excludedCreditCards = s.ExcludedCreditCards.SplitSafe(",");

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
            var paymentData = _httpContextAccessor.HttpContext.GetCheckoutState().PaymentData;
            model.CardholderName = (string)paymentData.Get("CardholderName");
            model.CardNumber = (string)paymentData.Get("CardNumber");
            model.CardCode = (string)paymentData.Get("CardCode");

            var creditCardType = (string)paymentData.Get("CreditCardType");
            var selectedCcType = model.CreditCardTypes.Where(x => x.Value.Equals(creditCardType, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedCcType != null)
            {
                selectedCcType.Selected = true;
            }
            
            var expireMonth = (string)paymentData.Get("ExpireMonth");
            var selectedMonth = model.ExpireMonths.Where(x => x.Value.Equals(expireMonth, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedMonth != null)
            {
                selectedMonth.Selected = true;
            }
            
            var expireYear = (string)paymentData.Get("ExpireYear");
            var selectedYear = model.ExpireYears.Where(x => x.Value.Equals(expireYear, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedYear != null)
            {
                selectedYear.Selected = true;
            }
            
            return View(model);
        }
    }
}
