global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Stripe;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements
{
    internal class Module : ModuleBase, IConfigurable, ICookiePublisher
    {
        private readonly IPaymentService _paymentService;
        private readonly Localizer T;

        public Module(IPaymentService paymentService, Localizer localizer)
        {
            _paymentService = paymentService;
            T = localizer;
        }

        // INFO: Update API Version when updating Stripe.net dll
        public static string ApiVersion => "2022-08-01";

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "StripeAdmin", new { area = "Admin" });

        public async Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
        {
            var store = Services.StoreContext.CurrentStore;

            var isActiveStripe = await _paymentService.IsPaymentProviderActiveAsync("Smartstore.StripeElements", null, store.Id);

            if (isActiveStripe)
            {
                var cookieInfo = new CookieInfo
                {
                    Name = T("Plugins.FriendlyName.Smartstore.Stripe"),
                    Description = T("Plugins.Smartstore.Stripe.CookieInfo"),
                    CookieType = CookieType.Required
                };

                return new List<CookieInfo> { cookieInfo }.AsEnumerable();
            }

            return null;
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<StripeSettings>();

            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<StripeSettings>();

            await base.UninstallAsync();
        }
    }
}