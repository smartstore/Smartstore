using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Media;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public abstract class OfflinePaymentViewComponentBase : SmartViewComponent
    {
        public abstract Task<IViewComponentResult> InvokeAsync(string providerName);

        protected async Task<TModel> GetPaymentInfoModelAsync<TModel, TSetting>(Action<TModel, TSetting> fn = null)
            where TModel : PaymentInfoModelBase, new()
            where TSetting : PaymentSettingsBase, new()
        {
            var services = HttpContext.RequestServices;
            var settings = services.GetRequiredService<TSetting>();
            var mediaService = services.GetRequiredService<IMediaService>();

            var model = new TModel
            {
                DescriptionText = GetLocalizedText(settings.DescriptionText),
                ThumbnailUrl = await mediaService.GetUrlAsync(settings.ThumbnailPictureId, 120, null, false)
            };

            fn?.Invoke(model, settings);

            return model;
        }

        private string GetLocalizedText(string text)
        {
            if (text.EmptyNull().StartsWith('@'))
            {
                return T(text[1..]);
            }

            return text;
        }
    }
}
