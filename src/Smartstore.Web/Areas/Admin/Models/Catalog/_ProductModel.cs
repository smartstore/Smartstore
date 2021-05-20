using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Search.Facets;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Validation;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ProductModel : ProductOverviewModel
    {
        // TODO: (mh) (core) Finish Smartstore.Admin.Models.Catalog.ProductModel
        // TODO: (core) What to do about AdditionalMetadataAttribute?

        [LocalizedDisplay("*AssociatedToProductName")]
        public int AssociatedToProductId { get; set; }

        [LocalizedDisplay("*.AssociatedToProductName")]
        public string AssociatedToProductName { get; set; }

        [LocalizedDisplay("*Visibility")]
        public ProductVisibility Visibility { get; set; }

        [LocalizedDisplay("*.Condition")]
        public ProductCondition Condition { get; set; }

        [LocalizedDisplay("*ProductTemplate")]
        public int ProductTemplateId { get; set; }
        //public IList<SelectListItem> AvailableProductTemplates { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }

        [LocalizedDisplay("*FullDescription")]
        public string FullDescription { get; set; }

        [LocalizedDisplay("*AdminComment")]
        public string AdminComment { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [LocalizedDisplay("*AllowCustomerReviews")]
        public bool AllowCustomerReviews { get; set; }

        [LocalizedDisplay("*ProductTags")]
        public string[] ProductTags { get; set; }
        //public MultiSelectList AvailableProductTags { get; set; }

        [LocalizedDisplay("*CountryOfOriginId")]
        public int? CountryOfOriginId { get; set; }
        //public IList<SelectListItem> AvailableCountries { get; set; }

        [LocalizedDisplay("*IsGiftCard")]
        public bool IsGiftCard { get; set; }

        [LocalizedDisplay("*GiftCardType")]
        public int GiftCardTypeId { get; set; }

        [LocalizedDisplay("*RequireOtherProducts")]
        public bool RequireOtherProducts { get; set; }

        [LocalizedDisplay("*RequiredProductIds")]
        public string RequiredProductIds { get; set; }

        [LocalizedDisplay("*AutomaticallyAddRequiredProducts")]
        public bool AutomaticallyAddRequiredProducts { get; set; }

        [LocalizedDisplay("*IsDownload")]
        public bool IsDownload { get; set; }

        [LocalizedDisplay("*NewVersionDownloadId")]
        [UIHint("Download")]
        public int? NewVersionDownloadId { get; set; }

        [LocalizedDisplay("Common.Download.Version")]
        public string NewVersion { get; set; }

        //public List<DownloadVersion> DownloadVersions { get; set; }

        [LocalizedDisplay("*Download")]
        [UIHint("Download")]
        public int? DownloadId { get; set; }

        [JsonIgnore]
        public string DownloadThumbUrl { get; set; }
        //public Download CurrentDownload { get; set; }

        [LocalizedDisplay("Common.Download.Version")]
        public string DownloadFileVersion { get; set; }

        [LocalizedDisplay("*UnlimitedDownloads")]
        public bool UnlimitedDownloads { get; set; }

        [LocalizedDisplay("*MaxNumberOfDownloads")]
        public int MaxNumberOfDownloads { get; set; }

        [LocalizedDisplay("*DownloadExpirationDays")]
        public int? DownloadExpirationDays { get; set; }

        [LocalizedDisplay("*DownloadActivationType")]
        public int DownloadActivationTypeId { get; set; }

        [LocalizedDisplay("*HasSampleDownload")]
        public bool HasSampleDownload { get; set; }

        [LocalizedDisplay("*SampleDownload")]
        [UIHint("Download")]
        public int? SampleDownloadId { get; set; }

        public int? OldSampleDownloadId { get; set; }

        [LocalizedDisplay("*HasUserAgreement")]
        public bool HasUserAgreement { get; set; }

        [LocalizedDisplay("*UserAgreementText")]
        public string UserAgreementText { get; set; }

        public string AddChangelog { get; set; }

        [LocalizedDisplay("*IsRecurring")]
        public bool IsRecurring { get; set; }

        [LocalizedDisplay("*RecurringCycleLength")]
        public int RecurringCycleLength { get; set; }

        [LocalizedDisplay("*RecurringCyclePeriod")]
        public int RecurringCyclePeriodId { get; set; }

        [LocalizedDisplay("*RecurringTotalCycles")]
        public int RecurringTotalCycles { get; set; }

        [LocalizedDisplay("*IsShipEnabled")]
        public bool IsShipEnabled { get; set; }

        [LocalizedDisplay("*IsFreeShipping")]
        public bool IsFreeShipping { get; set; }

        [LocalizedDisplay("*AdditionalShippingCharge")]
        public decimal AdditionalShippingCharge { get; set; }

        [LocalizedDisplay("*IsEsd")]
        public bool IsEsd { get; set; }

        [LocalizedDisplay("*TaxCategory")]
        public int? TaxCategoryId { get; set; }
        //public IList<SelectListItem> AvailableTaxCategories { get; set; }

        [LocalizedDisplay("*ManageInventoryMethod")]
        public int ManageInventoryMethodId { get; set; }
        //public IList<SelectListItem> AvailableManageInventoryMethods { get; set; }

        public int OriginalStockQuantity { get; set; }

        [LocalizedDisplay("*DisplayStockAvailability")]
        public bool DisplayStockAvailability { get; set; }

        [LocalizedDisplay("*DisplayStockQuantity")]
        public bool DisplayStockQuantity { get; set; }

        [LocalizedDisplay("*LowStockActivity")]
        public int LowStockActivityId { get; set; }

        [LocalizedDisplay("*NotifyAdminForQuantityBelow")]
        public int NotifyAdminForQuantityBelow { get; set; }

        [LocalizedDisplay("*BackorderMode")]
        public int BackorderModeId { get; set; }

        [LocalizedDisplay("*AllowBackInStockSubscriptions")]
        public bool AllowBackInStockSubscriptions { get; set; }

        [LocalizedDisplay("*OrderMinimumQuantity")]
        public int OrderMinimumQuantity { get; set; }

        [LocalizedDisplay("*OrderMaximumQuantity")]
        public int OrderMaximumQuantity { get; set; }

        [LocalizedDisplay("*QuantityStep")]
        public int QuantityStep { get; set; }

        [LocalizedDisplay("*QuantiyControlType")]
        public QuantityControlType QuantiyControlType { get; set; }

        [LocalizedDisplay("*HideQuantityControl")]
        public bool HideQuantityControl { get; set; }

        [LocalizedDisplay("*AllowedQuantities")]
        public string AllowedQuantities { get; set; }

        [LocalizedDisplay("*DisableBuyButton")]
        public bool DisableBuyButton { get; set; }

        [LocalizedDisplay("*DisableWishlistButton")]
        public bool DisableWishlistButton { get; set; }

        [LocalizedDisplay("*AvailableForPreOrder")]
        public bool AvailableForPreOrder { get; set; }

        [LocalizedDisplay("*CallForPrice")]
        public bool CallForPrice { get; set; }

        [LocalizedDisplay("*ProductCost")]
        public decimal ProductCost { get; set; }

        [LocalizedDisplay("*CustomerEntersPrice")]
        public bool CustomerEntersPrice { get; set; }

        [LocalizedDisplay("*MinimumCustomerEnteredPrice")]
        public decimal MinimumCustomerEnteredPrice { get; set; }

        [LocalizedDisplay("*MaximumCustomerEnteredPrice")]
        public decimal MaximumCustomerEnteredPrice { get; set; }

        [LocalizedDisplay("*BundleTitleText")]
        public string BundleTitleText { get; set; }

        [LocalizedDisplay("*BundlePerItemPricing")]
        public bool BundlePerItemPricing { get; set; }

        [LocalizedDisplay("*BundlePerItemShipping")]
        public bool BundlePerItemShipping { get; set; }

        [LocalizedDisplay("*BundlePerItemShoppingCart")]
        public bool BundlePerItemShoppingCart { get; set; }

        [LocalizedDisplay("*AttributeChoiceBehaviour")]
        public AttributeChoiceBehaviour AttributeChoiceBehaviour { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
        public string BaseDimensionIn { get; set; }
        public string BaseWeightIn { get; set; }

        //public IList<ProductLocalizedModel> Locales { get; set; }

        // ACL (customer roles).
        [UIHint("CustomerRoles")]
        //[AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        // Store mapping.
        [UIHint("Stores")]
        //[AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        public int NumberOfAvailableCategories { get; set; }
        public int NumberOfAvailableManufacturers { get; set; }
        public int NumberOfAvailableProductAttributes { get; set; }

        //Pictures.
        [LocalizedDisplay("*HasPreviewPicture")]
        public bool HasPreviewPicture { get; set; }
        //public ProductPictureModel AddPictureModel { get; set; }

        public IList<ProductMediaFile> ProductMediaFiles { get; set; }

        [UIHint("Discounts")]
        //[AdditionalMetadata("multiple", true)]
        //[AdditionalMetadata("discountType", DiscountType.AssignedToSkus)]
        [LocalizedDisplay("Admin.Promotions.Discounts.AppliedDiscounts")]
        public int[] SelectedDiscountIds { get; set; }

        //public AddProductSpecificationAttributeModel AddSpecificationAttributeModel { get; set; }

        //public CopyProductModel CopyProductModel { get; set; }

        //BasePrice
        [LocalizedDisplay("*BasePriceEnabled")]
        public bool BasePriceEnabled { get; set; }

        [LocalizedDisplay("*BasePriceMeasureUnit")]
        public string BasePriceMeasureUnit { get; set; }

        [LocalizedDisplay("*BasePriceAmount")]
        public decimal? BasePriceAmount { get; set; }

        [LocalizedDisplay("*BasePriceBaseAmount")]
        public int? BasePriceBaseAmount { get; set; }

        //public IList<SelectListItem> AvailableMeasureWeights { get; set; }
        //public IList<SelectListItem> AvailableMeasureUnits { get; set; }

        [UIHint("DeliveryTimes")]
        [LocalizedDisplay("*DeliveryTime")]
        public int? DeliveryTimeId { get; set; }

        [LocalizedDisplay("*QuantityUnit")]
        public int? QuantityUnitId { get; set; }
        //public IList<SelectListItem> AvailableQuantityUnits { get; set; }

        public string ProductSelectCheckboxClass { get; set; }

        public bool IsSystemProduct { get; set; }
        public string SystemName { get; set; }
    }

    public partial class ProductModelValidator : SmartValidator<ProductModel>
    {
        public ProductModelValidator(SmartDbContext db)
        {
            CopyFromEntityRules<Product>(db);
        }
    }
}