using Smartstore.Core.Configuration;

namespace Smartstore.OfflinePayment.Settings
{
    /// <summary>
    /// Represents manual payment processor transaction mode
    /// </summary>
    public enum TransactMode
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Authorize
        /// </summary>
        Authorize = 1,

        /// <summary>
        /// Paid
        /// </summary>
        Paid = 2
    }

    public abstract class PaymentSettingsBase : ISettings
    {
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }
    }

    public class CashOnDeliveryPaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class DirectDebitPaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class InvoicePaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class ManualPaymentSettings : PaymentSettingsBase, ISettings
    {
        public TransactMode TransactMode { get; set; }
        public string ExcludedCreditCards { get; set; }
    }

    public class PurchaseOrderNumberPaymentSettings : PaymentSettingsBase, ISettings
    {
        public TransactMode TransactMode { get; set; }
    }

    public class PayInStorePaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class PrepaymentPaymentSettings : PaymentSettingsBase, ISettings
    {
    }
}