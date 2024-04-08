using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart
{
    public partial class OrderTotalsModel : ModelBase
    {
        public bool IsEditable { get; set; }
        public int TotalQuantity { get; set; }

        public Money SubTotal { get; set; }
        public string SubtotalLabel { get; set; }
        public Money? SubTotalDiscount { get; set; }
        public bool AllowRemovingSubTotalDiscount { get; set; }
        public string Shipping { get; set; }
        public bool RequiresShipping { get; set; }
        public string SelectedShippingMethod { get; set; }
        public Money? PaymentMethodAdditionalFee { get; set; }
        public Money Tax { get; set; }
        public List<TaxRate> TaxRates { get; set; } = [];
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }
        public bool DisplayWeight { get; set; }
        public List<GiftCard> GiftCards { get; set; } = [];
        public Money OrderTotalDiscount { get; set; }
        public bool AllowRemovingOrderTotalDiscount { get; set; }
        public int RedeemedRewardPoints { get; set; }
        public string RedeemedRewardPointsAmount { get; set; }
        public string CreditBalance { get; set; }
        public Money? OrderTotalRounding { get; set; }
        public Money? OrderTotal { get; set; }
        public decimal Weight { get; set; }
        public string WeightMeasureUnitName { get; set; }
        public bool ShowConfirmOrderLegalHint { get; set; }

        public ShoppingCartSubtotal CartSubtotal { get; set; }
        public ShoppingCartTotal CartTotal { get; set; }

        public partial class TaxRate : ModelBase
        {
            public string Rate { get; set; }
            public Money Value { get; set; }
            public string Label { get; set; }
        }

        public partial class GiftCard : EntityModelBase
        {
            public string CouponCode { get; set; }
            public Money Amount { get; set; }
            public Money Remaining { get; set; }
        }
    }
}