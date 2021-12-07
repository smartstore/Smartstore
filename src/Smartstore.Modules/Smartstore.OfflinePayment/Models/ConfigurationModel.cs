using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Models
{
    public abstract class ConfigurationModelBase : ModelBase
    {
        public string PostActionName { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }

        [UIHint("TextArea")]
        [AdditionalMetadata("rows", 6)]
        [LocalizedDisplay("Plugins.SmartStore.OfflinePayment.DescriptionText")]
        public string DescriptionText { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [LocalizedDisplay("Plugins.SmartStore.OfflinePayment.PaymentMethodLogo")]
        [UIHint("Media"), AdditionalMetadata("album", "content"), AdditionalMetadata("transientUpload", true)]
        public int PaymentMethodLogo { get; set; }
    }

    public class CashOnDeliveryConfigurationModel : ConfigurationModelBase
    {
    }

    public class DirectDebitConfigurationModel : ConfigurationModelBase
    {
    }

    public class InvoiceConfigurationModel : ConfigurationModelBase
    {
    }

    public class ManualConfigurationModel : ConfigurationModelBase
    {
        [LocalizedDisplay("Plugins.Payments.Manual.Fields.TransactMode")]
        public TransactMode TransactMode { get; set; }
        public List<SelectListItem> TransactModeValues { get; set; } = new();

        [LocalizedDisplay("Plugins.Payments.Manual.ExcludedCreditCards")]
        public string[] ExcludedCreditCards { get; set; }
        public List<SelectListItem> AvailableCreditCards { get; set; } = new();
    }

    public class PayInStoreConfigurationModel : ConfigurationModelBase
    {
    }

    public class PrepaymentConfigurationModel : ConfigurationModelBase
    {
    }

    public class PurchaseOrderNumberConfigurationModel : ConfigurationModelBase
    {
    }
}