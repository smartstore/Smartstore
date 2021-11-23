using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class PurchaseOrderNumberViewComponent : OfflinePaymentViewComponentBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PurchaseOrderNumberViewComponent(
            IComponentContext ctx,
            IMediaService mediaService,
            IHttpContextAccessor httpContextAccessor) : base(ctx, mediaService)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            var model = await PaymentInfoGetAsync<PurchaseOrderNumberPaymentInfoModel, PurchaseOrderNumberPaymentSettings>();

            var paymentData = _httpContextAccessor.HttpContext.GetCheckoutState().PaymentData;

            model.PurchaseOrderNumber = (string)paymentData.Get("PurchaseOrderNumber");

            return View(model);
        }
    }
}
