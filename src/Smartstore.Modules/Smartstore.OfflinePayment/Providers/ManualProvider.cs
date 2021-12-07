using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    [SystemName("Payments.Manual")]
    [FriendlyName("Credit Card (manual)")]
    [Order(1)]
    public class ManualProvider : OfflinePaymentProviderBase<ManualPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(ManualPaymentViewComponent);
        }

        protected override string GetProviderName()
        {
            return nameof(ManualProvider);
        }

        public RouteInfo GetConfigurationRoute()
            => new("ManualConfigure", "OfflinePayment", new { area = "Admin" });

        public static List<SelectListItem> CreditCardTypes
        {
            get
            {
                var creditCardTypes = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Visa", Value = "Visa" },
                    new SelectListItem { Text = "Master Card", Value = "MasterCard" },
                    new SelectListItem { Text = "Discover", Value = "Discover" },
                    new SelectListItem { Text = "Amex", Value = "Amex" }
                };

                return creditCardTypes;
            }
        }

        private async Task<ProcessPaymentResult> GetProcessPaymentResultAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var settings = await CommonServices.SettingFactory.LoadSettingsAsync<ManualPaymentSettings>(processPaymentRequest.StoreId);

            result.AllowStoringCreditCardNumber = true;

            switch (settings.TransactMode)
            {
                case TransactMode.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case TransactMode.Authorize:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case TransactMode.Paid:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                default:
                    result.Errors.Add(T("Common.Payment.TranactionTypeNotSupported"));
                    return result;
            }

            return result;
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return await GetProcessPaymentResultAsync(processPaymentRequest);
        }

        public override async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return await GetProcessPaymentResultAsync(processPaymentRequest);
        }

        public override Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        public override bool RequiresInteraction => true;

        public override RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Manual;
    }
}