global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Net.Http;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Smartstore.Core.Localization;
global using Smartstore.PayPal.Models;
global using Smartstore.PayPal.Settings;
global using Smartstore.Web.Modelling;
global using System.Linq;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Engine.Modularity;

namespace Smartstore.PayPal
{
    internal class Module : ModuleBase, ICookiePublisher
    {
        public static string PartnerId => "D9X8D9DSNFZBU";
        public static string PartnerClientId => "AbTCZEJtQBJTYwXMK5W4p0-WDfKx9wEoT0xdMAF7OldhDF36aK5k1tD-9eW3LKBE7CKpyilyBh-VDlMF";

        private readonly IPaymentService _paymentService;
        
        public Module(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
        {
            var store = Services.StoreContext.CurrentStore;
            var isActivePayPalStandard = await _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalStandard", null, store.Id);
            var isActivePayPalInvoice = await _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalPayUponInvoice", null, store.Id);

            if (isActivePayPalStandard || isActivePayPalInvoice)
            {
                var cookieInfo = new CookieInfo
                {
                    Name = T("Plugins.FriendlyName.Smartstore.PayPal"),
                    Description = T("Plugins.Smartstore.PayPal.CookieInfo"),
                    CookieType = CookieType.Required
                };

                return new List<CookieInfo> { cookieInfo }.AsEnumerable();
            }

            return null;
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<PayPalSettings>();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<PayPalSettings>();
            await base.UninstallAsync();
        }
    }
}
