using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Components;

namespace Smartstore.OfflinePayment.Components
{
    public abstract class OfflinePaymentViewComponentBase : SmartViewComponent
    {
        public abstract IViewComponentResult Invoke(string providerName);

        protected async Task<TModel> CreatePaymentInfoModelAsync<TModel>(string providerName)
            where TModel : PaymentInfoModelBase, new()
        {
            var db = HttpContext.RequestServices.GetRequiredService<SmartDbContext>();
            var paymentMethod = await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentMethodSystemName == providerName);

            var model = new TModel
            {
                Description = paymentMethod?.FullDescription
            };

            return model;
        }

        protected TModel GetPaymentInfoModel<TModel, TSetting>(Action<TModel, TSetting> fn = null)
            where TModel : PaymentInfoModelBase, new()
            where TSetting : PaymentSettingsBase, new()
        {
            var services = HttpContext.RequestServices;
            var settings = services.GetRequiredService<TSetting>();

            var model = new TModel
            {
                Description = GetLocalizedText(settings.DescriptionText)
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
