using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Common;

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
        public decimal OrderSubtotalInclTax { get; set; }
        public string OrderSubtotalInclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubtotalExclTax")]
        public decimal OrderSubtotalExclTax { get; set; }
        public string OrderSubtotalExclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountInclTax")]
        public decimal OrderSubTotalDiscountInclTax { get; set; }
        public string OrderSubTotalDiscountInclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountExclTax")]
        public decimal OrderSubTotalDiscountExclTax { get; set; }
        public string OrderSubTotalDiscountExclTaxString { get; set; }

        [LocalizedDisplay("*OrderShippingInclTax")]
        public decimal OrderShippingInclTax { get; set; }
        public string OrderShippingInclTaxString { get; set; }

        [LocalizedDisplay("*OrderShippingExclTax")]
        public decimal OrderShippingExclTax { get; set; }
        public string OrderShippingExclTaxString { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeInclTax")]
        public decimal PaymentMethodAdditionalFeeInclTax { get; set; }
        public string PaymentMethodAdditionalFeeInclTaxString { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeExclTax")]
        public decimal PaymentMethodAdditionalFeeExclTax { get; set; }
        public string PaymentMethodAdditionalFeeExclTaxString { get; set; }

        [LocalizedDisplay("*Tax")]
        public decimal Tax { get; set; }
        public string TaxString { get; set; }

        public List<TaxRate> TaxRates { get; set; }
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }

        public TaxDisplayType TaxDisplayType { get; set; }
        public bool AllowCustomersToSelectTaxDisplayType { get; set; }

        [LocalizedDisplay("*OrderTotalDiscount")]
        public decimal OrderDiscount { get; set; }
        public string OrderDiscountString { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public int RedeemedRewardPoints { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public string RedeemedRewardPointsAmountString { get; set; }

        [LocalizedDisplay("*CreditBalance")]
        public decimal CreditBalance { get; set; }
        public string CreditBalanceString { get; set; }

        [LocalizedDisplay("*OrderTotalRounding")]
        public decimal OrderTotalRounding { get; set; }
        public string OrderTotalRoundingString { get; set; }

        [LocalizedDisplay("*RefundedAmount")]
        public string RefundedAmountString { get; set; }

        [LocalizedDisplay("*PartialRefund.AmountToRefund")]
        public decimal AmountToRefund { get; set; }
        public decimal MaxAmountToRefund { get; set; }
        public string MaxAmountToRefundString { get; set; }

        public bool AllowStoringCreditCardNumber { get; set; }
        public bool AllowStoringDirectDebit { get; set; }

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

        public bool DisplayCompletePaymentNote { get; set; }
        public bool DisplayPurchaseOrderNumber { get; set; }

        [LocalizedDisplay("*PurchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; }
        
        [LocalizedDisplay("*AuthorizationTransactionID")]
        public string AuthorizationTransactionId { get; set; }
        
        [LocalizedDisplay("*CaptureTransactionID")]
        public string CaptureTransactionId { get; set; }
        
        [LocalizedDisplay("*SubscriptionTransactionID")]
        public string SubscriptionTransactionId { get; set; }

        [LocalizedDisplay("*AuthorizationTransactionResult")]
        public string AuthorizationTransactionResult { get; set; }
        
        [LocalizedDisplay("*CaptureTransactionResult")]
        public string CaptureTransactionResult { get; set; }

        public string ShippingAddressGoogleMapsUrl { get; set; }
        public bool CanAddNewShipments { get; set; }

        [LocalizedDisplay("*AcceptThirdPartyEmailHandOver")]
        public bool AcceptThirdPartyEmailHandOver { get; set; }

        public bool HasDownloadableProducts { get; set; }
        public string CustomerOrderComment { get; set; }
        public string CheckoutAttributeInfo { get; set; }
        public bool DisplayPdfInvoice { get; set; }

        [LocalizedDisplay("Admin.Orders.OrderNotes.Fields.AddOrderNoteDisplayToCustomer")]
        public bool AddOrderNoteDisplayToCustomer { get; set; }
        
        [LocalizedDisplay("Admin.Orders.OrderNotes.Fields.AddOrderNoteMessage")]
        public string AddOrderNoteMessage { get; set; }

        [LocalizedDisplay("*RecurringPayment")]
        public int RecurringPaymentId { get; set; }

        public bool CanCancelOrder { get; set; }
        public bool CanCompleteOrder { get; set; }
        public bool CanCapture { get; set; }
        public bool CanMarkOrderAsPaid { get; set; }
        public bool CanRefund { get; set; }
        public bool CanRefundOffline { get; set; }
        public bool CanPartiallyRefund { get; set; }
        public bool CanPartiallyRefundOffline { get; set; }
        public bool CanVoid { get; set; }
        public bool CanVoidOffline { get; set; }

        [LocalizedDisplay("*BillingAddress")]
        public AddressModel BillingAddress { get; set; }

        [LocalizedDisplay("*ShippingAddress")]
        public AddressModel ShippingAddress { get; set; }

        public List<GiftCard> GiftCards { get; set; }
        public List<OrderItemModel> Items { get; set; }

        public string AutoUpdateOrderItemInfo { get; set; }
        public AutoUpdateOrderItemModel AutoUpdateOrderItem { get; set; }

        #region Nested classes

        public class TaxRate : ModelBase
        {
            public string Rate { get; set; }
            public string Value { get; set; }
        }

        public class GiftCard : ModelBase
        {
            [LocalizedDisplay("Admin.Orders.Fields.GiftCardInfo")]
            public string CouponCode { get; set; }
            public string Amount { get; set; }
        }

        public class ReturnRequestModel : EntityModelBase
        {
            public int Quantity { get; set; }
            public ReturnRequestStatus Status { get; set; }
            public string StatusString { get; set; }
            public string StatusLabel
            {
                get
                {
                    if (Status >= ReturnRequestStatus.RequestRejected)
                        return "warning";

                    if (Status >= ReturnRequestStatus.ReturnAuthorized)
                        return "success";

                    if (Status == ReturnRequestStatus.Received)
                        return "info";

                    if (Status == ReturnRequestStatus.Pending)
                        return "danger";

                    return "light";
                }
            }
        }

        public class BundleItemModel : ModelBase
        {
            public int ProductId { get; set; }
            public string Sku { get; set; }
            public string ProductName { get; set; }
            public string ProductSeName { get; set; }
            public bool VisibleIndividually { get; set; }
            public int Quantity { get; set; }
            public int DisplayOrder { get; set; }
            public string PriceWithDiscount { get; set; }
            public string AttributeInfo { get; set; }
        }

        [LocalizedDisplay("Admin.Orders.OrderNotes.Fields.")]
        public class OrderNote : EntityModelBase
        {
            public int OrderId { get; set; }

            [LocalizedDisplay("*DisplayToCustomer")]
            public bool DisplayToCustomer { get; set; }

            [LocalizedDisplay("*Note")]
            public string Note { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        public class UploadLicenseModel : ModelBase
        {
            public int OrderId { get; set; }
            public int OrderItemId { get; set; }

            [UIHint("Download")]
            public int LicenseDownloadId { get; set; }
            public int OldLicenseDownloadId { get; set; }
        }

        public class OrderItemModel : EntityModelBase
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string Sku { get; set; }
            public ProductType ProductType { get; set; }
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }

            public decimal UnitPriceInclTax { get; set; }
            public string UnitPriceInclTaxString { get; set; }

            public decimal UnitPriceExclTax { get; set; }
            public string UnitPriceExclTaxString { get; set; }

            public decimal TaxRate { get; set; }
            public int Quantity { get; set; }

            public decimal DiscountInclTax { get; set; }
            public string DiscountInclTaxString { get; set; }

            public decimal DiscountExclTax { get; set; }
            public string DiscountExclTaxString { get; set; }

            public decimal SubTotalInclTax { get; set; }
            public string SubTotalInclTaxString { get; set; }

            public decimal SubTotalExclTax { get; set; }
            public string SubTotalExclTaxString { get; set; }

            public string AttributeInfo { get; set; }
            public string RecurringInfo { get; set; }
            public List<int> PurchasedGiftCardIds { get; set; }

            public bool IsDownload { get; set; }
            public int DownloadCount { get; set; }
            public DownloadActivationType DownloadActivationType { get; set; }
            public bool IsDownloadActivated { get; set; }
            public int? LicenseDownloadId { get; set; }

            public bool BundlePerItemPricing { get; set; }
            public bool BundlePerItemShoppingCart { get; set; }

            public List<BundleItemModel> BundleItems { get; set; }
            public List<ReturnRequestModel> ReturnRequests { get; set; }

            public bool IsReturnRequestPossible
                => !(ReturnRequests?.Any() ?? false) || ReturnRequests.Sum(x => x.Quantity) < Quantity;
        }

        #endregion
    }
}
