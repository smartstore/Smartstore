global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Smartstore.Core.Localization;
global using Smartstore.Web.Modelling;
using Smartstore.Engine.Modularity;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.Payment
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await SaveSettingsAsync<CashOnDeliveryPaymentSettings>();
            await SaveSettingsAsync<InvoicePaymentSettings>();
            await SaveSettingsAsync<PayInStorePaymentSettings>();
            await SaveSettingsAsync<PrepaymentPaymentSettings>();
            await SaveSettingsAsync<ManualPaymentSettings>();
            await SaveSettingsAsync<DirectDebitPaymentSettings>();
            await SaveSettingsAsync<PurchaseOrderNumberPaymentSettings>();

            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<CashOnDeliveryPaymentSettings>();
            await DeleteSettingsAsync<InvoicePaymentSettings>();
            await DeleteSettingsAsync<PayInStorePaymentSettings>();
            await DeleteSettingsAsync<PrepaymentPaymentSettings>();
            await DeleteSettingsAsync<ManualPaymentSettings>();
            await DeleteSettingsAsync<DirectDebitPaymentSettings>();
            await DeleteSettingsAsync<PurchaseOrderNumberPaymentSettings>();

            await DeleteLanguageResourcesAsync();
            await DeleteLanguageResourcesAsync("Plugins.Payment.CashOnDelivery");
            await DeleteLanguageResourcesAsync("Plugins.Payment.Invoice");
            await DeleteLanguageResourcesAsync("Plugins.Payment.PayInStore");
            await DeleteLanguageResourcesAsync("Plugins.Payment.Prepayment");
            await DeleteLanguageResourcesAsync("Plugins.Payment.Manual");
            await DeleteLanguageResourcesAsync("Plugins.Payment.DirectDebit");

            await base.UninstallAsync();
        }
    }
}
