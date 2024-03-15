using System.Runtime.Serialization;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Orders
{
    public partial class OrderDetailsModel : EntityModelBase
    {
        public int StoreId { get; set; }
        public CompanyInformationSettings MerchantCompanyInfo { get; set; } = new();
        public string MerchantCompanyCountryName { get; set; }
        public string OrderNumber { get; set; }
        public bool DisplayPdfInvoice { get; set; }
        public bool RenderOrderNotes { get; set; }
        public DateTime CreatedOn { get; set; }
        public string OrderStatus { get; set; }
        public bool IsReOrderAllowed { get; set; }
        public bool IsReturnRequestAllowed { get; set; }
        public bool IsShippable { get; set; }
        public string ShippingStatus { get; set; }
        public AddressModel BillingAddress { get; set; }
        public AddressModel ShippingAddress { get; set; }
        public string ShippingMethod { get; set; }
        public List<ShipmentBriefModel> Shipments { get; set; } = [];
        public string VatNumber { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentMethodSystemName { get; set; }

        [IgnoreDataMember]
        public Order Order { get; set; }

        #region Offline payment data 

        public string CardNumber { get; set; }
        public string MaskedCreditCardNumber { get; set; }
        public string CardCvv2 { get; set; }
        public string CardExpirationMonth { get; set; }
        public string CardExpirationYear { get; set; }
        public string DirectDebitAccountHolder { get; set; }
        public string DirectDebitAccountNumber { get; set; }
        public string DirectDebitBankCode { get; set; }
        public string DirectDebitBankName { get; set; }
        public string DirectDebitBIC { get; set; }
        public string DirectDebitCountry { get; set; }
        public string DirectDebitIban { get; set; }

        #endregion

        public bool CanRePostProcessPayment { get; set; }
        public bool DisplayPurchaseOrderNumber { get; set; }
        public string PurchaseOrderNumber { get; set; }

        public Money OrderSubtotal { get; set; }
        public Money OrderSubTotalDiscount { get; set; }
        public Money OrderShipping { get; set; }
        public Money PaymentMethodAdditionalFee { get; set; }
        public string CheckoutAttributeInfo { get; set; }
        public Money Tax { get; set; }
        public List<TaxRate> TaxRates { get; set; } = [];
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }
        public Money OrderTotalDiscount { get; set; }
        public int RedeemedRewardPoints { get; set; }
        public Money RedeemedRewardPointsAmount { get; set; }
        public Money CreditBalance { get; set; }
        public Money OrderTotalRounding { get; set; }
        public Money OrderTotal { get; set; }
        public string CustomerComment { get; set; }
        public int CustomerLanguageId { get; set; }
        public bool ShowSku { get; set; }
        public bool ShowProductImages { get; set; }
        public bool ShowProductBundleImages { get; set; }
        public int BundleThumbSize { get; set; }
        public List<GiftCard> GiftCards { get; set; } = [];
        public List<OrderItemModel> Items { get; set; } = [];
        public List<OrderNote> OrderNotes { get; set; } = [];

        #region Nested Classes

        public partial class OrderItemModel : EntityModelBase
        {
            public string Sku { get; set; }
            public int ProductId { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public ProductType ProductType { get; set; }
            public Money UnitPrice { get; set; }
            public Money SubTotal { get; set; }
            public int Quantity { get; set; }
            public string QuantityUnit { get; set; }
            public string AttributeInfo { get; set; }
            public bool BundlePerItemPricing { get; set; }
            public bool BundlePerItemShoppingCart { get; set; }
            public ImageModel Image { get; set; }
            public List<BundleItemModel> BundleItems { get; set; } = [];
        }

        public partial class BundleItemModel : ModelBase
        {
            public ImageModel Image { get; set; }
            public string Sku { get; set; }
            public string ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public bool VisibleIndividually { get; set; }
            public int Quantity { get; set; }
            public int DisplayOrder { get; set; }
            public Money PriceWithDiscount { get; set; }
            public string AttributeInfo { get; set; }
            public bool HideThumbnail { get; set; }
        }

        public partial class TaxRate : ModelBase
        {
            public decimal Rate { get; set; }
            public string FormattedRate { get; set; }
            public Money Amount { get; set; }
            public string Label { get; set; }
        }

        public partial class GiftCard : ModelBase
        {
            public Money Amount { get; set; }
            public string FormattedAmount { get; set; }
            public Money Remaining { get; set; }
            public string CouponCode { get; set; }
        }

        public partial class OrderNote : ModelBase
        {
            public string Note { get; set; }
            public DateTime CreatedOn { get; set; }
            public string FriendlyCreatedOn { get; set; }
        }

        public partial class ShipmentBriefModel : EntityModelBase
        {
            public string TrackingNumber { get; set; }
            public DateTime? ShippedDate { get; set; }
            public DateTime? DeliveryDate { get; set; }
        }

        #endregion
    }
}
