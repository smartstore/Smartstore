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
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class ManualPaymentViewComponent : SmartViewComponent
    {
        private readonly IComponentContext _ctx;
        private readonly IMediaService _mediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public ManualPaymentViewComponent(
            IComponentContext ctx,
            IMediaService mediaService,
            IHttpContextAccessor httpContextAccessor)
        {
            _ctx = ctx;
            _mediaService = mediaService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
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

        // TODO: (mh) (core) Helper functions or base class???
        private async Task<TModel> PaymentInfoGetAsync<TModel, TSetting>(Action<TModel, TSetting> fn = null)
            where TModel : PaymentInfoModelBase, new()
            where TSetting : PaymentSettingsBase, new()
        {
            var settings = _ctx.Resolve<TSetting>();
            var model = new TModel
            {
                DescriptionText = GetLocalizedText(settings.DescriptionText),
                ThumbnailUrl = await _mediaService.GetUrlAsync(settings.ThumbnailPictureId, 120, null, false)
            };

            fn?.Invoke(model, settings);

            return model;
        }

        private string GetLocalizedText(string text)
        {
            if (text.EmptyNull().StartsWith("@"))
            {
                return T(text[1..]);
            }

            return text;
        }
    }
}
