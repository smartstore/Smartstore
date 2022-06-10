using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin.Models.Payments
{
    [LocalizedDisplay("Admin.Configuration.Payment.Methods.Fields.")]
    public class PaymentMethodModel : ProviderModel, IActivatable
    {
        public bool IsActive { get; set; }

        [LocalizedDisplay("*SupportCapture")]
        public bool SupportCapture { get; set; }

        [LocalizedDisplay("*SupportPartiallyRefund")]
        public bool SupportPartiallyRefund { get; set; }

        [LocalizedDisplay("*SupportRefund")]
        public bool SupportRefund { get; set; }

        [LocalizedDisplay("*SupportVoid")]
        public bool SupportVoid { get; set; }

        [LocalizedDisplay("*RecurringPaymentType")]
        public string RecurringPaymentType { get; set; }
    }
}
