using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment.Models
{
    public abstract class ConfigurationModelBase : ModelBase
    {
        public string PostActionName { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [LocalizedDisplay("Plugins.Payments.Manual.Fields.TransactMode")]
        public TransactMode TransactMode { get; set; }
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
        [LocalizedDisplay("Plugins.Payments.Manual.ExcludedCreditCards")]
        public string[] ExcludedCreditCards { get; set; }
        public List<SelectListItem> AvailableCreditCards { get; set; } = [];
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