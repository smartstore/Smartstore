using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class PurchaseOrderNumberViewComponent : SmartViewComponent
    {
        private readonly IComponentContext _ctx;
        private readonly IMediaService _mediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public PurchaseOrderNumberViewComponent(
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
            var model = await PaymentInfoGetAsync<PurchaseOrderNumberPaymentInfoModel, PurchaseOrderNumberPaymentSettings>();

            var paymentData = _httpContextAccessor.HttpContext.GetCheckoutState().PaymentData;

            model.PurchaseOrderNumber = (string)paymentData.Get("PurchaseOrderNumber");

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
