global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Smartstore.Core.Localization;
global using Smartstore.PayPal.Models;
global using Smartstore.PayPal.Settings;
global using Smartstore.Web.Modelling;
using Smartstore.Core.Identity;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal
{
    internal class Module : ModuleBase, ICookiePublisher
    {
        public static string PartnerId => "FW39AHL99XEVN";
        public static string PartnerClientId => "AWDFq4wYkqD_VwBfNqNU_TuhPhNc3i18fwGb3JwhyH1MNvQnwNmtpSyG1r8RK95JEzS8UDz8wyYV28qq";

        private readonly PayPalHelper _payPalHelper;

        public Module(PayPalHelper payPalHelper)
        {
            _payPalHelper = payPalHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
        {
            var isActiveStandard = await _payPalHelper.IsPayPalStandardActiveAsync();
            var isActiveInvoice = await _payPalHelper.IsPayUponInvoiceActiveAsync();
            var isActiveCreditCard = await _payPalHelper.IsCreditCardActiveAsync();
            var isActivePayLater = await _payPalHelper.IsPayLaterActiveAsync();
            var isActiveSepa = await _payPalHelper.IsSepaActiveAsync();

            // INFO: APMs don't need cookies as everything on page is handled via API requests.
            // The pages to which the customer is redirected when using APMs must handle cookie consent themsleves.
            if (isActiveStandard || isActiveInvoice || isActiveCreditCard || isActivePayLater || isActiveSepa)
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