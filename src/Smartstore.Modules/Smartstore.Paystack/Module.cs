using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Paystack.Configuration;
using Smartstore.Scheduling;

namespace Smartstore.Paystack
{
    public class Module:ModuleBase
    {
        private readonly ITaskStore _taskStore;
        private readonly IPaymentService _paymentService;
        private readonly IProviderManager _providerManager;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

        public Module(
            ITaskStore taskStore,
            IPaymentService paymentService,
            IProviderManager providerManager,
            ExternalAuthenticationSettings externalAuthenticationSettings)
        {
            _taskStore = taskStore;
            _paymentService = paymentService;
            _providerManager = providerManager;
            _externalAuthenticationSettings = externalAuthenticationSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<PaystackSettings>();
            await ImportLanguageResourcesAsync();

           
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<PaystackSettings>();
            await DeleteLanguageResourcesAsync();

            await base.UninstallAsync();
        }
    }
}
