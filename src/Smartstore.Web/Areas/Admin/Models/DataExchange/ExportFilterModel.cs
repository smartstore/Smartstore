using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Admin.Models.Export
{
    [LocalizedDisplay("Admin.DataExchange.Export.Filter.")]
    public class ExportFilterModel
    {
        #region All entity types

        [UIHint("Stores")]
        [LocalizedDisplay("*StoreId")]
        public int? StoreId { get; set; }

        [LocalizedDisplay("*CreatedFrom")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? CreatedFrom { get; set; }

        [LocalizedDisplay("*CreatedTo")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? CreatedTo { get; set; }

        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("*CustomerRoleIds")]
        public int[] CustomerRoleIds { get; set; }

        #endregion

        #region Product

        [LocalizedDisplay("*IsPublished")]
        public bool? IsPublished { get; set; }

        [LocalizedDisplay("*ProductType")]
        public ProductType? ProductType { get; set; }

        [LocalizedDisplay("*Visibility")]
        public ProductVisibility? Visibility { get; set; }

        [LocalizedDisplay("*IdMinimum")]
        [AdditionalMetadata("invariant", true)]
        public int? IdMinimum { get; set; }

        [LocalizedDisplay("*IdMaximum")]
        [AdditionalMetadata("invariant", true)]
        public int? IdMaximum { get; set; }

        [LocalizedDisplay("*PriceMinimum")]
        public decimal? PriceMinimum { get; set; }

        [LocalizedDisplay("*PriceMaximum")]
        public decimal? PriceMaximum { get; set; }

        [LocalizedDisplay("*AvailabilityMinimum")]
        public int? AvailabilityMinimum { get; set; }

        [LocalizedDisplay("*AvailabilityMaximum")]
        public int? AvailabilityMaximum { get; set; }

        [LocalizedDisplay("*ProductTagId")]
        public int? ProductTagId { get; set; }

        [LocalizedDisplay("*FeaturedProducts")]
        public bool? FeaturedProducts { get; set; }

        [LocalizedDisplay("*WithoutManufacturers")]
        public bool? WithoutManufacturers { get; set; }

        [LocalizedDisplay("*ManufacturerId")]
        public int? ManufacturerId { get; set; }

        [LocalizedDisplay("*WithoutCategories")]
        public bool? WithoutCategories { get; set; }

        [LocalizedDisplay("*CategoryIds")]
        public int[] CategoryIds { get; set; }

        [LocalizedDisplay("*CategoryIds")]
        public int? CategoryId { get; set; }

        [LocalizedDisplay("*IncludeSubCategories")]
        public bool IncludeSubCategories { get; set; }

        #endregion

        #region Customer

        [LocalizedDisplay("*IsActiveCustomer")]
        public bool? IsActiveCustomer { get; set; }

        [LocalizedDisplay("*IsTaxExempt")]
        public bool? IsTaxExempt { get; set; }

        [LocalizedDisplay("*BillingCountryIds")]
        public int[] BillingCountryIds { get; set; }

        [LocalizedDisplay("*ShippingCountryIds")]
        public int[] ShippingCountryIds { get; set; }

        [LocalizedDisplay("*LastActivityFrom")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? LastActivityFrom { get; set; }

        [LocalizedDisplay("*LastActivityTo")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? LastActivityTo { get; set; }

        [LocalizedDisplay("*HasSpentAtLeastAmount")]
        public decimal? HasSpentAtLeastAmount { get; set; }

        [LocalizedDisplay("*HasPlacedAtLeastOrders")]
        public int? HasPlacedAtLeastOrders { get; set; }

        #endregion

        #region Order

        [LocalizedDisplay("*OrderStatusIds")]
        public int[] OrderStatusIds { get; set; }

        [LocalizedDisplay("*PaymentStatusIds")]
        public int[] PaymentStatusIds { get; set; }

        [LocalizedDisplay("*ShippingStatusIds")]
        public int[] ShippingStatusIds { get; set; }

        #endregion

        #region Newsletter Subscription

        [LocalizedDisplay("*IsActiveSubscriber")]
        public bool? IsActiveSubscriber { get; set; }

        [LocalizedDisplay("*WorkingLanguageId")]
        public int? WorkingLanguageId { get; set; }

        #endregion

        #region Shopping Cart Items

        [LocalizedDisplay("*ShoppingCartTypeId")]
        public int? ShoppingCartTypeId { get; set; }

        #endregion
    }
}
