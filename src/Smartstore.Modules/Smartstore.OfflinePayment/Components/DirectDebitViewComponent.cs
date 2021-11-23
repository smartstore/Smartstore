using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class DirectDebitViewComponent : OfflinePaymentViewComponentBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DirectDebitViewComponent(
            IComponentContext ctx, 
            IMediaService mediaService, 
            IHttpContextAccessor httpContextAccessor) : base(ctx, mediaService)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await PaymentInfoGetAsync<DirectDebitPaymentInfoModel, DirectDebitPaymentSettings>();
            var paymentData = _httpContextAccessor.HttpContext.GetCheckoutState().PaymentData;
            
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