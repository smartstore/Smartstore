using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Shipments.")]
    public class ShipmentModel : EntityModelBase
    {
        public int StoreId { get; set; }
        public int LanguageId { get; set; }

        [ValidateNever]
        public AddressModel ShippingAddress { get; set; } = new();

        [LocalizedDisplay("Admin.Orders.Fields.ShippingMethod")]
        public string ShippingMethod { get; set; }

        public CompanyInformationSettings MerchantCompanyInfo { get; set; } = new();
        public string FormattedMerchantAddress { get; set; }

        [LocalizedDisplay("Admin.Orders.Fields.OrderNumber")]
        public string OrderNumber { get; set; }
        public string PurchaseOrderNumber { get; set; }

        [LocalizedDisplay("*OrderID")]
        public int OrderId { get; set; }

        [LocalizedDisplay("*TotalWeight")]
        public decimal? TotalWeight { get; set; }
        [LocalizedDisplay("*TotalWeight")]
        public string TotalWeightString { get; set; }

        [LocalizedDisplay("*TrackingNumber")]
        public string TrackingNumber { get; set; }

        [LocalizedDisplay("*TrackingUrl")]
        public string TrackingUrl { get; set; }

        [LocalizedDisplay("*ShippedDate")]
        public DateTime? ShippedDate { get; set; }
        public bool CanShip { get; set; }

        [LocalizedDisplay("*DeliveryDate")]
        public DateTime? DeliveryDate { get; set; }
        public bool CanDeliver { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        public List<ShipmentItemModel> Items { get; set; } = new();

        public string EditUrl { get; set; }
        public string OrderEditUrl { get; set; }

        #region Nested classes

        public class ShipmentItemModel : EntityModelBase
        {
            public int OrderItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public ProductType ProductType { get; set; }
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }
            public string Sku { get; set; }
            public string Gtin { get; set; }
            public string AttributeInfo { get; set; }
            public bool BundlePerItemPricing { get; set; }
            public bool BundlePerItemShoppingCart { get; set; }

            // Weight of one item (product).
            public string ItemWeight { get; set; }
            public string ItemDimensions { get; set; }

            public int QuantityToAdd { get; set; }
            public int QuantityOrdered { get; set; }
            public int QuantityInThisShipment { get; set; }
            public int QuantityInAllShipments { get; set; }

            public List<BundleItemModel> BundleItems { get; set; } = new();
        }

        public class BundleItemModel : ModelBase
        {
            public string Sku { get; set; }
            public string ProductName { get; set; }
            public string ProductSeName { get; set; }
            public bool VisibleIndividually { get; set; }
            public int Quantity { get; set; }
            public int DisplayOrder { get; set; }
            public string AttributeInfo { get; set; }
        }

        #endregion
    }

    public partial class ShipmentValidator : AbstractValidator<ShipmentModel>
    {
        public ShipmentValidator(Localizer T)
        {
            RuleFor(x => x.TrackingUrl)
                .Must(x => x.IsEmpty() || x.IsWebUrl())
                .WithMessage(T("Admin.Validation.Url"));
        }
    }
}
