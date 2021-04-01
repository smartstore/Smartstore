using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class OrderTotalsModel : ModelBase
    {        
        public bool IsEditable { get; set; }
        public string SubTotal { get; set; } // TODO: (ms) (core) Make "Money" later
        public string SubTotalDiscount { get; set; } // TODO: (ms) (core) Make "Money" later
        public bool AllowRemovingSubTotalDiscount { get; set; }
        public string Shipping { get; set; }
        public bool RequiresShipping { get; set; }
        public string SelectedShippingMethod { get; set; }
        public string PaymentMethodAdditionalFee { get; set; } // TODO: (ms) (core) Make "Money" later
        public string Tax { get; set; }
        public List<TaxRate> TaxRates { get; set; } = new();
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }
        public bool DisplayWeight { get; set; }
        public List<GiftCard> GiftCards { get; set; } = new();
        public string OrderTotalDiscount { get; set; } // TODO: (ms) (core) Make "Money" later
        public bool AllowRemovingOrderTotalDiscount { get; set; }
        public int RedeemedRewardPoints { get; set; }
        public string RedeemedRewardPointsAmount { get; set; }
        public string CreditBalance { get; set; }
        public string OrderTotalRounding { get; set; } // TODO: (ms) (core) Make "Money" later
        public string OrderTotal { get; set; } // TODO: (ms) (core) Make "Money" later
        public decimal Weight { get; set; }
        public string WeightMeasureUnitName { get; set; }
        public bool ShowConfirmOrderLegalHint { get; set; }

        public partial class TaxRate : ModelBase
        {
            public string Rate { get; set; }
            public string Value { get; set; }
            public string Label { get; set; }
        }

        public partial class GiftCard : EntityModelBase
        {
            public string CouponCode { get; set; }
            public string Amount { get; set; }
            public string Remaining { get; set; }
        }
    }
}