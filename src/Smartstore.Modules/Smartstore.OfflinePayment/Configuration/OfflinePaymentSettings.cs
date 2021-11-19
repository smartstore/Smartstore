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
        public string DescriptionText { get; set; }
        public int ThumbnailPictureId { get; set; }
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }
    }

    public class CashOnDeliveryPaymentSettings : PaymentSettingsBase, ISettings
    {
        public CashOnDeliveryPaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.CashOnDelivery.PaymentInfoDescription";
        }
    }

    public class DirectDebitPaymentSettings : PaymentSettingsBase, ISettings
    {
        public DirectDebitPaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.DirectDebit.PaymentInfoDescription";
        }
    }

    public class InvoicePaymentSettings : PaymentSettingsBase, ISettings
    {
        public InvoicePaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.Invoice.PaymentInfoDescription";
        }
    }

    public class ManualPaymentSettings : PaymentSettingsBase, ISettings
    {
        public ManualPaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.Manual.PaymentInfoDescription";
        }

        public TransactMode TransactMode { get; set; }
        public string ExcludedCreditCards { get; set; }
    }

    public class PurchaseOrderNumberPaymentSettings : PaymentSettingsBase, ISettings
    {
        public PurchaseOrderNumberPaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.PurchaseOrderNumber.PaymentInfoDescription";
        }

        public TransactMode TransactMode { get; set; }
    }

    public class PayInStorePaymentSettings : PaymentSettingsBase, ISettings
    {
        public PayInStorePaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.PayInStore.PaymentInfoDescription";
        }
    }

    public class PrepaymentPaymentSettings : PaymentSettingsBase, ISettings
    {
        public PrepaymentPaymentSettings()
        {
            DescriptionText = "@Plugins.Payment.Prepayment.PaymentInfoDescription";
        }
    }
}