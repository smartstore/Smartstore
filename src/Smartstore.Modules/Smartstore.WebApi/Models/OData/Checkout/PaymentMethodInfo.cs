using Smartstore.Core.Checkout.Payment;

namespace Smartstore.Web.Api.Models.Checkout
{
    public partial class PaymentMethodInfo
    {
        /// <summary>
        /// A value that indicates whether the payment method requires user input in checkout
        /// before proceeding, e.g. credit card or direct debit payment. Default is <c>false</c>.
        /// </summary>
        public bool RequiresInteraction { get; set; }

        /// <summary>
        /// A value that indicates whether the payment method requires the payment selection page in checkout
        /// before proceeding. For example, to create a payment transaction at this stage.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RequiresPaymentSelection { get; set; }

        /// <summary>
        /// A value that indicates whether (later) capturing of the payment amount is supported,
        /// for instance when the goods are shipped.
        /// </summary>
        public bool SupportCapture { get; set; }

        /// <summary>
        /// A value that indicates whether a partial refund is supported.
        /// </summary>
        public bool SupportPartiallyRefund { get; set; }

        /// <summary>
        /// A value that indicates whether a full refund is supported.
        /// </summary>
        public bool SupportRefund { get; set; }

        /// <summary>
        /// A value that indicates whether cancellation of the payment (transaction) is supported.
        /// </summary>
        public bool SupportVoid { get; set; }

        /// <summary>
        /// The type of recurring payment.
        /// </summary>
        public RecurringPaymentType RecurringPaymentType { get; set; }

        /// <summary>
        /// The payment method type.
        /// </summary>
        public PaymentMethodType PaymentMethodType { get; set; }
    }
}
