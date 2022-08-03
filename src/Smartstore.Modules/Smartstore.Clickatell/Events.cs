using Smartstore.Clickatell.Services;
using Smartstore.Clickatell.Settings;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Events;

namespace Smartstore.Clickatell
{
    public class Events : IConsumer
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task HandleEventAsync(OrderPlacedEvent message,
            ClickatellHttpClient client,
            ICommonServices services,
            ClickatellSettings clickatellSettings,
            IProviderManager providerManager,
            CancellationToken cancelToken)
        {
            if (!clickatellSettings.Enabled)
            {
                return;
            }

            var module = services.ApplicationContext.ModuleCatalog.GetModuleByAssembly(typeof(Events).Assembly);

            await client.SendSmsAsync(
                T("Plugins.Sms.Clickatell.OrderPlacedMessage", message.Order.GetOrderNumber()),
                clickatellSettings,
                cancelToken);

            message.Order.AddOrderNote(T("Plugins.Sms.Clickatell.SmsSentNote"));
            await services.DbContext.SaveChangesAsync(cancelToken);
        }
    }
}
