using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public class OfflinePaymentGenericViewComponent : SmartViewComponent
    {
        private readonly IComponentContext _ctx;
        private readonly IMediaService _mediaService;
        
        public OfflinePaymentGenericViewComponent(IComponentContext ctx, IMediaService mediaService)
        {
            _ctx = ctx;
            _mediaService = mediaService;
        }

        // TODO: (mh) (core) Try to pass settings class instead of providerName. Then the whole switch statement can be spared.
        public async Task<IViewComponentResult> InvokeAsync(string providerName)
        {
            switch (providerName)
            {
                case "CashOnDeliveryProvider":
                    var cashOnDeliveryPaymentInfoModel = await PaymentInfoGetAsync<CashOnDeliveryPaymentInfoModel, CashOnDeliveryPaymentSettings>();
                    return View(cashOnDeliveryPaymentInfoModel);
                case "InvoiceProvider":
                    var invoicePaymentInfoModel = await PaymentInfoGetAsync<InvoicePaymentInfoModel, InvoicePaymentSettings>();
                    return View(invoicePaymentInfoModel);
                case "PayInStoreProvider":
                    var payInStorePaymentInfoModel = await PaymentInfoGetAsync<PayInStorePaymentInfoModel, PayInStorePaymentSettings>();
                    return View(payInStorePaymentInfoModel);
                case "PrepaymentProvider":
                    var prepaymentPaymentInfoModel = await PaymentInfoGetAsync<PrepaymentPaymentInfoModel, PrepaymentPaymentSettings>();
                    return View(prepaymentPaymentInfoModel);
            }

            return View();
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
