using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Components
{
    public class GenericPaymentViewComponent : OfflinePaymentViewComponentBase
    {
        public GenericPaymentViewComponent(IComponentContext ctx, IMediaService mediaService) : base(ctx, mediaService)
        {
        }

        // TODO: (mh) (core) Try to pass settings class instead of providerName. Then the whole switch statement can be spared.
        public override async Task<IViewComponentResult> InvokeAsync(string providerName)
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
    }
}
