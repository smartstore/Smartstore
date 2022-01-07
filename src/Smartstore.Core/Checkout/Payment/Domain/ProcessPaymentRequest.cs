using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a payment info holder.
    /// </summary>
    public partial class ProcessPaymentRequest : PaymentRequest
    {
        /// <summary>
        /// Gets or sets a store identifier.
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets a customer.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets an order unique identifier. Used when order is not saved yet (payment gateways that do not redirect a customer to a third-party URL).
        /// </summary>
        public Guid OrderGuid { get; set; }

        /// <summary>
        /// Gets or sets an order total.
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets an order tax total.
        /// </summary>
        public decimal OrderTax { get; set; }

        /// <summary>
        /// Gets or sets a payment method identifier.
        /// </summary>
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a payment method identifier.
        /// </summary>
        public bool IsMultiOrder { get; set; }

        // TODO: (mg) (core) later remove obsolete, commented out code.
        // Obsolete: use CheckoutState and PostProcessPayment instead if you want to insert order attributes for payments.
        // (Was only used by Payone plugin in the early days anyway.)
        /// <summary>Use that dictionary for any payment method or checkout flow specific data.</summary>
        //public Dictionary<string, CustomPaymentRequestValue> CustomProperties { get; set; } = new();

        /// <summary>
        /// All shopping cart item identifiers.
        /// </summary>
        public List<int> ShoppingCartItemIds { get; set; } = new();

        #region Payment method specific properties 

        /// <summary>
        /// Gets or sets a credit card type (Visa, Master Card, etc...).
        /// </summary>
        public string CreditCardType { get; set; }

        /// <summary>
        /// Gets or sets a credit card owner name.
        /// </summary>
        public string CreditCardName { get; set; }

        /// <summary>
        /// Gets or sets a credit card number.
        /// </summary>
        public string CreditCardNumber { get; set; }

        /// <summary>
        /// Gets or sets a credit card expire year.
        /// </summary>
        public int CreditCardExpireYear { get; set; }

        /// <summary>
        /// Gets or sets a credit card expire month.
        /// </summary>
        public int CreditCardExpireMonth { get; set; }

        /// <summary>
        /// Gets or sets a credit card CVV2 (Card Verification Value).
        /// </summary>
        public string CreditCardCvv2 { get; set; }

        /// <summary>
        /// Gets or sets a paypal payer token (required for Paypal payment methods).
        /// </summary>
        public string PaypalToken { get; set; }

        /// <summary>
        /// Gets or sets a paypal payer identifier (required for Paypal payment methods).
        /// </summary>
        public string PaypalPayerId { get; set; }

        /// <summary>
        /// Gets or sets a google order number (required for Google Checkout).
        /// </summary>
        public string GoogleOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets a purchase order number (required for Purchase Order payment method).
        /// </summary>
        public string PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the credit card start year.
        /// </summary>
        public int CreditCardStartYear { get; set; }

        /// <summary>
        /// Gets or sets the credit card start month.
        /// </summary>
        public int CreditCardStartMonth { get; set; }

        /// <summary>
        /// Gets or sets the credit card issue number.
        /// </summary>
        public string CreditCardIssueNumber { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account holder.
        /// </summary>
        public string DirectDebitAccountHolder { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account number.
        /// </summary>
        public string DirectDebitAccountNumber { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank code.
        /// </summary>
        public string DirectDebitBankCode { get; set; }

        /// <summary>
        /// Gets or sets the direct debit country.
        /// </summary>
        public string DirectDebitCountry { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank name.
        /// </summary>
        public string DirectDebitBankName { get; set; }

        /// <summary>
        /// Gets or sets the direct debit IBAN.
        /// </summary>
        public string DirectDebitIban { get; set; }

        /// <summary>
        /// Gets or sets the direct debit BIC.
        /// </summary>
        public string DirectDebitBic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whetger the shipping method is set.
        /// </summary>
        public bool IsShippingMethodSet { get; set; }

        #endregion

        #region Recurring payments

        /// <summary>
        /// Gets or sets a value idicating whether it's a recurring payment (initial payment was already processed).
        /// </summary>
        public bool IsRecurringPayment { get; set; }

        /// <summary>
        /// Gets or sets an initial (parent) order identifier if order is recurring.
        /// </summary>
        public int InitialOrderId { get; set; }

        /// <summary>
        /// Gets or sets the cycle length.
        /// </summary>
        public int RecurringCycleLength { get; set; }

        /// <summary>
        /// Gets or sets the cycle period.
        /// </summary>
        public RecurringProductCyclePeriod RecurringCyclePeriod { get; set; }

        /// <summary>
        /// Gets or sets the total cycles.
        /// </summary>
        public int RecurringTotalCycles { get; set; }

        #endregion
    }

    //public partial class CustomPaymentRequestValue
    //{
    //    /// <summary>
    //    /// The value of the custom property.
    //    /// </summary>
    //    public object Value { get; set; }

    //    /// <summary>
    //    /// A value indicating whether to automatically create a generic attribute if an order has been placed.
    //    /// </summary>
    //    public bool AutoCreateGenericAttribute { get; set; }
    //}
}
