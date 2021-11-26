using System.Threading.Tasks;
using Smartstore.Clickatell.Services;
using Smartstore.Clickatell.Settings;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Events;

namespace Smartstore.Clickatell
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ClickatellSettings _clickatellSettings;

        public Events(SmartDbContext db, ICommonServices services, ClickatellSettings clickatellSettings)
        {
            _db = db;
            _services = services;
            _clickatellSettings = clickatellSettings;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public async Task HandleEventAsync(OrderPlacedEvent message)
        {
            if (!_clickatellSettings.Enabled)
                return;

            // TODO: (mh) (core) How to get out of here if the Plugin isn't active? Lets discuss with MC first.
            //var descriptor = _pluginFinder.GetPluginDescriptorBySystemName(ClickatellSmsProvider.SystemName);
            //if (descriptor == null)
            //    return;

            //var storeId = _services.StoreContext.CurrentStore.Id;

            //if (!(storeId == 0 || _services.Settings.GetSettingByKey<string>(descriptor.GetSettingKey("LimitedToStores")).ToIntArrayContains(storeId, true)))
            //    return;

            //var plugin = descriptor.Instance() as ClickatellSmsProvider;
            //if (plugin == null)
            //    return;

            try
            {
                await ClickatellSmsSender.SendSmsAsync(_clickatellSettings, T("Plugins.Sms.Clickatell.OrderPlacedMessage", message.Order.GetOrderNumber()));

                message.Order.AddOrderNote(T("Plugins.Sms.Clickatell.SmsSentNote"));
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }
}
