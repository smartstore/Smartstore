using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public partial class OrderModel : EditOrderModel
    {
        [LocalizedDisplay("*CustomerIP")]
        public string CustomerIp { get; set; }

        [LocalizedDisplay("*Affiliate")]
        public int AffiliateId { get; set; }
        public string AffiliateFullName { get; set; }

        [LocalizedDisplay("*OrderSubtotalInclTax")]
        public string OrderSubtotalInclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubtotalExclTax")]
        public string OrderSubtotalExclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountInclTax")]
        public string OrderSubTotalDiscountInclTaxString { get; set; }

        [LocalizedDisplay("*OrderSubTotalDiscountExclTax")]
        public string OrderSubTotalDiscountExclTaxString { get; set; }

        [LocalizedDisplay("*OrderShippingInclTax")]
        public string OrderShippingInclTaxString { get; set; }

        [LocalizedDisplay("*OrderShippingExclTax")]
        public string OrderShippingExclTaxString { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeInclTax")]
        public string PaymentMethodAdditionalFeeInclTaxString { get; set; }

        [LocalizedDisplay("*PaymentMethodAdditionalFeeExclTax")]
        public string PaymentMethodAdditionalFeeExclTaxString { get; set; }

        [LocalizedDisplay("*Tax")]
        public string OrderTaxString { get; set; }

        public List<TaxRate> TaxRatesList { get; set; }

        [LocalizedDisplay("*OrderTotalDiscount")]
        public string OrderDiscountString { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public int RedeemedRewardPoints { get; set; }

        [LocalizedDisplay("*RedeemedRewardPoints")]
        public string RedeemedRewardPointsAmountString { get; set; }

        public string CreditBalanceString { get; set; }

        [LocalizedDisplay("*OrderTotalRounding")]
        public string OrderTotalRoundingString { get; set; }

        [LocalizedDisplay("*RefundedAmount")]
        public string RefundedAmountString { get; set; }

        public bool AllowStoringCreditCardNumber { get; set; }
        public bool AllowStoringDirectDebit { get; set; }

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
        [ValidateNever]
        public AddressModel BillingAddress { get; set; } = new();

        [LocalizedDisplay("*ShippingAddress")]
        [ValidateNever]
        public AddressModel ShippingAddress { get; set; }

        public List<GiftCard> GiftCards { get; set; }
        public List<OrderItemModel> Items { get; set; }

        public string UpdateOrderItemInfo { get; set; }
        public UpdateOrderItemModel UpdateOrderItem { get; set; }

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
            public int GiftCardId { get; set; }
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

        public class RefundModel : EntityModelBase
        {
            [LocalizedDisplay("Admin.Orders.Fields.PartialRefund.AmountToRefund")]
            public decimal AmountToRefund { get; set; }
            public decimal MaxAmountToRefund { get; set; }
            public string MaxAmountToRefundString { get; set; }
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
            public string AttributesInfo { get; set; }
        }

        [LocalizedDisplay("Admin.Orders.OrderNotes.Fields.")]
        public class OrderNote : EntityModelBase
        {
            public int OrderId { get; set; }

            [LocalizedDisplay("*DisplayToCustomer")]
            public bool DisplayToCustomer { get; set; }

            [UIHint("Textarea"), AdditionalMetadata("rows", 4)]
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

            public decimal PriceInclTax { get; set; }
            public string PriceInclTaxString { get; set; }

            public decimal PriceExclTax { get; set; }
            public string PriceExclTaxString { get; set; }

            public decimal TaxRate { get; set; }
            public int Quantity { get; set; }

            public decimal DiscountAmountInclTax { get; set; }
            public string DiscountAmountInclTaxString { get; set; }

            public decimal DiscountAmountExclTax { get; set; }
            public string DiscountAmountExclTaxString { get; set; }

            public string AttributeDescription { get; set; }
            public string RecurringInfo { get; set; }

            public bool IsDownload { get; set; }
            public int DownloadCount { get; set; }
            public DownloadActivationType DownloadActivationType { get; set; }
            public bool IsDownloadActivated { get; set; }
            public int? LicenseDownloadId { get; set; }

            public bool BundlePerItemPricing { get; set; }
            public bool BundlePerItemShoppingCart { get; set; }

            public List<BundleItemModel> BundleItems { get; set; } = new();
            public List<ReturnRequestModel> ReturnRequests { get; set; } = new();
            public List<int> PurchasedGiftCardIds { get; set; } = new();

            public bool IsReturnRequestPossible
                => !(ReturnRequests?.Any() ?? false) || ReturnRequests.Sum(x => x.Quantity) < Quantity;
        }

        #endregion
    }
}
