using Smartstore.Core.Configuration;

namespace Smartstore.OfflinePayment.Settings
{
    /// <summary>
    /// Represents the payment status after order completion.
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

        public TransactMode TransactMode { get; set; }
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
        public string ExcludedCreditCards { get; set; }
    }

    public class PurchaseOrderNumberPaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class PayInStorePaymentSettings : PaymentSettingsBase, ISettings
    {
    }

    public class PrepaymentPaymentSettings : PaymentSettingsBase, ISettings
    {
    }
}