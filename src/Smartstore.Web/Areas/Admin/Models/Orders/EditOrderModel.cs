namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public class EditOrderModel : OrderOverviewModel
    {
        #region Edit order totals

        [LocalizedDisplay("*Edit.OrderSubtotal")]
        public decimal OrderSubtotalInclTax { get; set; }

        [LocalizedDisplay("*Edit.OrderSubtotal")]
        public decimal OrderSubtotalExclTax { get; set; }

        [LocalizedDisplay("*Edit.OrderSubTotalDiscount")]
        public decimal OrderSubTotalDiscountInclTax { get; set; }

        [LocalizedDisplay("*Edit.OrderSubTotalDiscount")]
        public decimal OrderSubTotalDiscountExclTax { get; set; }

        [LocalizedDisplay("*Edit.OrderShipping")]
        public decimal OrderShippingInclTax { get; set; }

        [LocalizedDisplay("*Edit.OrderShipping")]
        public decimal OrderShippingExclTax { get; set; }

        [LocalizedDisplay("*Edit.PaymentMethodAdditionalFee")]
        public decimal PaymentMethodAdditionalFeeInclTax { get; set; }

        [LocalizedDisplay("*Edit.PaymentMethodAdditionalFee")]
        public decimal PaymentMethodAdditionalFeeExclTax { get; set; }

        [LocalizedDisplay("*Edit.TaxRates")]
        public string TaxRates { get; set; }

        [LocalizedDisplay("*Edit.Tax")]
        public decimal OrderTax { get; set; }

        [LocalizedDisplay("*Edit.OrderTotalDiscount")]
        public decimal OrderDiscount { get; set; }

        [LocalizedDisplay("*CreditBalance")]
        public decimal CreditBalance { get; set; }

        [LocalizedDisplay("*OrderTotalRounding")]
        public decimal OrderTotalRounding { get; set; }

        [LocalizedDisplay("*Edit.OrderTotal")]
        public decimal OrderTotal { get; set; }

        #endregion

        #region Edit direct debit info

        [LocalizedDisplay("*DirectDebitAccountHolder")]
        public string DirectDebitAccountHolder { get; set; }

        [LocalizedDisplay("*DirectDebitAccountNumber")]
        public string DirectDebitAccountNumber { get; set; }

        [LocalizedDisplay("*DirectDebitBankCode")]
        public string DirectDebitBankCode { get; set; }

        [LocalizedDisplay("*DirectDebitBankName")]
        public string DirectDebitBankName { get; set; }

        [LocalizedDisplay("*DirectDebitBIC")]
        public string DirectDebitBIC { get; set; }

        [LocalizedDisplay("*DirectDebitCountry")]
        public string DirectDebitCountry { get; set; }

        [LocalizedDisplay("*DirectDebitIban")]
        public string DirectDebitIban { get; set; }

        #endregion

        #region Edit credit card info

        [LocalizedDisplay("*CardType")]
        public string CardType { get; set; }

        [LocalizedDisplay("*CardName")]
        public string CardName { get; set; }

        [LocalizedDisplay("*CardNumber")]
        public string CardNumber { get; set; }

        [LocalizedDisplay("*CardCVV2")]
        public string CardCvv2 { get; set; }

        [LocalizedDisplay("*CardExpirationMonth")]
        public string CardExpirationMonth { get; set; }

        [LocalizedDisplay("*CardExpirationYear")]
        public string CardExpirationYear { get; set; }

        #endregion
    }
}
