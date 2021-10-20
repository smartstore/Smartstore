using System;
using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public partial class OrderModel : OrderOverviewModel
    {
        [LocalizedDisplay("*CustomerIP")]
        public string CustomerIp { get; set; }

        [LocalizedDisplay("*Affiliate")]
        public int AffiliateId { get; set; }
        public string AffiliateFullName { get; set; }

        [LocalizedDisplay("*OrderSubtotalInclTax")]
        public string OrderSubtotalInclTax { get; set; }

        [LocalizedDisplay("*OrderSubtotalExclTax")]
        public string OrderSubtotalExclTax { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountInclTax")]
        public string OrderSubTotalDiscountInclTax { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountExclTax")]
        public string OrderSubTotalDiscountExclTax { get; set; }

        [LocalizedDisplay("*OrderShippingInclTax")]
        public string OrderShippingInclTax { get; set; }

        [LocalizedDisplay("*OrderShippingExclTax")]
        public string OrderShippingExclTax { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeInclTax")]
        public string PaymentMethodAdditionalFeeInclTax { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeExclTax")]
        public string PaymentMethodAdditionalFeeExclTax { get; set; }

        [LocalizedDisplay("*Tax")]
        public string Tax { get; set; }
        public List<TaxRate> TaxRates { get; set; }
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }

        [LocalizedDisplay("*OrderTotalDiscount")]
        public string OrderTotalDiscount { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public int RedeemedRewardPoints { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public string RedeemedRewardPointsAmount { get; set; }

        [LocalizedDisplay("*CreditBalance")]
        public string CreditBalance { get; set; }

        [LocalizedDisplay("*OrderTotalRounding")]
        public string OrderTotalRounding { get; set; }

        [LocalizedDisplay("*RefundedAmount")]
        public string RefundedAmount { get; set; }

        [LocalizedDisplay("*PartialRefund.AmountToRefund")]
        public decimal AmountToRefund { get; set; }
        public decimal MaxAmountToRefund { get; set; }
        public string MaxAmountToRefundFormatted { get; set; }


        //...

        #region Nested classes

        public class TaxRate : ModelBase
        {
            public string Rate { get; set; }
            public string Value { get; set; }
        }

        #endregion
    }
}
