using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;

// TODO: (mg) way too many classes for one file. Move all attribute models to a subfolder "Attributes"
// after Conditional Attributes are completed. No need to make all these "nested".
// TODO: (mg) DRY: ProductModel.ProductVariantAttributeValueModel -> ProductAttributeOptionModelBase.
namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ProductModel : ProductOverviewModel, ILocalizedModel<ProductLocalizedModel>
    {
        [LocalizedDisplay("*AssociatedToProductName")]
        public int AssociatedToProductId { get; set; }

        [LocalizedDisplay("*AssociatedToProductName")]
        public string AssociatedToProductName { get; set; }

        [LocalizedDisplay("*Visibility")]
        public ProductVisibility Visibility { get; set; }

        [LocalizedDisplay("*Condition")]
        public ProductCondition Condition { get; set; }

        [LocalizedDisplay("*ProductTemplate")]
        public int ProductTemplateId { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }

        [UIHint("Html")]
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
        public string[] ProductTagNames { get; set; }

        [LocalizedDisplay("*CountryOfOriginId")]
        public int? CountryOfOriginId { get; set; }

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

        [UIHint("Download")]
        [LocalizedDisplay("*NewVersionDownloadId")]
        public int? NewVersionDownloadId { get; set; }

        [LocalizedDisplay("Common.Download.Version")]
        public string NewVersion { get; set; }
        public List<DownloadVersion> DownloadVersions { get; set; } = new();

        [UIHint("Download")]
        [LocalizedDisplay("*Download")]
        public int? DownloadId { get; set; }

        [IgnoreDataMember]
        public string DownloadThumbUrl { get; set; }
        public Download CurrentDownload { get; set; }
        public MediaFileInfo CurrentFile { get; set; }

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

        [UIHint("Download")]
        [LocalizedDisplay("*SampleDownload")]
        public int? SampleDownloadId { get; set; }

        public int? OldSampleDownloadId { get; set; }

        [LocalizedDisplay("*HasUserAgreement")]
        public bool HasUserAgreement { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*UserAgreementText")]
        public string UserAgreementText { get; set; }

        [UIHint("Html")]
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
        public bool IsShippingEnabled { get; set; }

        [LocalizedDisplay("*IsFreeShipping")]
        public bool IsFreeShipping { get; set; }

        [LocalizedDisplay("*AdditionalShippingCharge")]
        public decimal? AdditionalShippingCharge { get; set; }

        [LocalizedDisplay("*IsEsd")]
        public bool IsEsd { get; set; }

        [LocalizedDisplay("*TaxCategory")]
        public int? TaxCategoryId { get; set; }

        [LocalizedDisplay("*ManageInventoryMethod")]
        public int ManageInventoryMethodId { get; set; }
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

        [LocalizedDisplay("*QuantityControlType")]
        public QuantityControlType QuantityControlType { get; set; }

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
        public decimal? ProductCost { get; set; }

        [LocalizedDisplay("*CustomerEntersPrice")]
        public bool CustomerEntersPrice { get; set; }

        [LocalizedDisplay("*MinimumCustomerEnteredPrice")]
        public decimal? MinimumCustomerEnteredPrice { get; set; }

        [LocalizedDisplay("*MaximumCustomerEnteredPrice")]
        public decimal? MaximumCustomerEnteredPrice { get; set; }

        [LocalizedDisplay("*BundleTitleText")]
        public string BundleTitleText { get; set; }

        [LocalizedDisplay("*BundlePerItemPricing")]
        public bool BundlePerItemPricing { get; set; }

        [LocalizedDisplay("*BundlePerItemShipping")]
        public bool BundlePerItemShipping { get; set; }

        [LocalizedDisplay("*BundlePerItemShoppingCart")]
        public bool BundlePerItemShoppingCart { get; set; }

        [IgnoreDataMember]
        public bool IsBundleWithItemPricing
            => ProductTypeId == (int)ProductType.BundledProduct && BundlePerItemPricing;

        [LocalizedDisplay("*AttributeChoiceBehaviour")]
        public AttributeChoiceBehaviour AttributeChoiceBehaviour { get; set; }

        [LocalizedDisplay("*AttributeCombinationRequired")]
        public bool AttributeCombinationRequired { get; set; }

        [LocalizedDisplay("*ComparePriceLabelId")]
        public int? ComparePriceLabelId { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
        public string BaseDimensionIn { get; set; }
        public string BaseWeightIn { get; set; }

        public List<ProductLocalizedModel> Locales { get; set; } = [];

        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        public int NumberOfAvailableCategories { get; set; }
        public int NumberOfAvailableManufacturers { get; set; }
        public int NumberOfAvailableProductAttributes { get; set; }

        //Pictures.
        [LocalizedDisplay("*HasPreviewPicture")]
        public bool HasPreviewPicture { get; set; }
        public ProductPictureModel AddPictureModel { get; set; } = new();
        public List<ProductMediaFile> ProductMediaFiles { get; set; } = [];

        [UIHint("Discounts")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("discountType", DiscountType.AssignedToSkus)]
        [LocalizedDisplay("Admin.Promotions.Discounts.AppliedDiscounts")]
        public int[] SelectedDiscountIds { get; set; }

        public AddProductSpecificationAttributeModel AddSpecificationAttributeModel { get; set; } = new();
        public GroupedProductConfigurationModel GroupedProductConfiguration { get; set; } = new();

        //BasePrice
        [LocalizedDisplay("*BasePriceEnabled")]
        public bool BasePriceEnabled { get; set; }

        [LocalizedDisplay("*BasePriceMeasureUnit")]
        public string BasePriceMeasureUnit { get; set; }

        [LocalizedDisplay("*BasePriceAmount")]
        public decimal? BasePriceAmount { get; set; }

        [LocalizedDisplay("*BasePriceBaseAmount")]
        public int? BasePriceBaseAmount { get; set; }

        [LocalizedDisplay("*QuantityUnit")]
        public int? QuantityUnitId { get; set; }
        public string ProductSelectCheckboxClass { get; set; }
        public bool IsSystemProduct { get; set; }
        public string SystemName { get; set; }

        #region Nested classes

        [LocalizedDisplay("Admin.Catalog.Products.SpecificationAttributes.Fields.")]
        public class AddProductSpecificationAttributeModel : ModelBase
        {
            [LocalizedDisplay("*SpecificationAttribute")]
            public int? SpecificationAttributeId { get; set; }

            [LocalizedDisplay("*SpecificationAttributeOption")]
            public int? SpecificationAttributeOptionId { get; set; }

            [LocalizedDisplay("*AllowFiltering")]
            public bool? AllowFiltering { get; set; }

            [LocalizedDisplay("*ShowOnProductPage")]
            public bool? ShowOnProductPage { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.Manufacturers.Fields.")]
        public class ProductManufacturerModel : EntityModelBase
        {
            public int ProductId { get; set; }
            public string EditUrl { get; set; }

            [LocalizedDisplay("*Manufacturer")]
            [UIHint("ProductManufacturer")]
            public int ManufacturerId { get; set; }
            public string Manufacturer { get; set; }

            [LocalizedDisplay("*IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.")]
        public class CrossSellProductModel : EntityModelBase
        {
            public int ProductId2 { get; set; }
            public string EditUrl { get; set; }

            [LocalizedDisplay("*CrossSells.Fields.Product")]
            public string Product2Name { get; set; }

            [LocalizedDisplay("*Fields.ProductType")]
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }

            [LocalizedDisplay("*Fields.Sku")]
            public string Product2Sku { get; set; }

            [LocalizedDisplay("*Fields.Published")]
            public bool Product2Published { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.")]
        public class RelatedProductModel : EntityModelBase
        {
            public int ProductId2 { get; set; }
            public string EditUrl { get; set; }

            [LocalizedDisplay("*RelatedProducts.Fields.Product")]
            public string Product2Name { get; set; }

            [LocalizedDisplay("*Fields.ProductType")]
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("*Fields.Sku")]
            public string Product2Sku { get; set; }

            [LocalizedDisplay("*Fields.Published")]
            public bool Product2Published { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
        public partial class AssociatedProductModel : EntityModelBase
        {
            [LocalizedDisplay("Admin.Catalog.Products.AssociatedProducts.Fields.Product")]
            public string Name { get; set; }

            [LocalizedDisplay("*ProductType")]
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }
            public string EditUrl { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("*Sku")]
            public string Sku { get; set; }

            [LocalizedDisplay("*Published")]
            public bool Published { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.BundleItems.Fields.")]
        public partial class BundleItemModel : EntityModelBase
        {
            public int ProductId { get; set; }

            [LocalizedDisplay("*Product")]
            public string ProductName { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.Fields.ProductType")]
            public string ProductTypeName { get; set; }
            public string ProductTypeLabelHint { get; set; }
            public string ProductEditUrl { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.Fields.Sku")]
            public string Sku { get; set; }

            [LocalizedDisplay("*Quantity")]
            public int Quantity { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("*Discount")]
            public decimal? Discount { get; set; }

            [LocalizedDisplay("*Visible")]
            public bool Visible { get; set; }

            [LocalizedDisplay("*Published")]
            public bool Published { get; set; }
        }

        public class ProductPictureModel : EntityModelBase
        {
            public int ProductId { get; set; }

            [UIHint("Media"), AdditionalMetadata("album", "catalog"), AdditionalMetadata("typeFilter", "image,video")]
            [LocalizedDisplay("Admin.Catalog.Products.Pictures.Fields.Picture")]
            public int PictureId { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.Pictures.Fields.Picture")]
            public string PictureUrl { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            public ProductMediaFile ProductMediaFile { get; set; }
        }

        public class ProductCategoryModel : EntityModelBase
        {
            public int ProductId { get; set; }
            public string EditUrl { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.Categories.Fields.Category")]
            [UIHint("ProductCategory")]
            public int CategoryId { get; set; }
            public string Category { get; set; }
            public string[] CategoryList { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.Categories.Fields.IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("Admin.Rules.AddedByRule")]
            public bool IsSystemMapping { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.TierPrices.Fields.")]
        public class TierPriceModel : EntityModelBase
        {
            public int ProductId { get; set; }

            [LocalizedDisplay("*CustomerRole")]
            [UIHint("TierPriceCustomer")]
            public int? CustomerRoleId { get; set; }
            public string CustomerRole { get; set; }

            [LocalizedDisplay("*Store")]
            [UIHint("TierPriceStore")]
            public int? StoreId { get; set; }
            public string Store { get; set; }

            [LocalizedDisplay("*Quantity")]
            public int Quantity { get; set; }

            // We don't name it Price to avoid model binding conflicts with product property.
            [LocalizedDisplay("*Price")]
            public decimal? Price1 { get; set; }

            [LocalizedDisplay("*CalculationMethod")]
            [UIHint("TierPriceCalculationMethod")]
            public int CalculationMethodId { get; set; }
            public string CalculationMethod { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Fields.")]
        public class ProductVariantAttributeModel : EntityModelBase
        {
            public int ProductId { get; set; }

            [LocalizedDisplay("*Attribute")]
            [UIHint("ProductAttribute")]
            public int ProductAttributeId { get; set; }
            public string ProductAttribute { get; set; }

            [LocalizedDisplay("*TextPrompt")]
            public string TextPrompt { get; set; }

            [LocalizedDisplay("*CustomData")]
            public string CustomData { get; set; }

            [LocalizedDisplay("*IsRequired")]
            public bool IsRequired { get; set; }

            [LocalizedDisplay("Admin.Catalog.Attributes.AttributeControlType")]
            [UIHint("AttributeControlType")]
            public int AttributeControlTypeId { get; set; }
            public string AttributeControlType { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values")]
            public string EditUrl { get; set; }
            public string EditLinkText { get; set; }
            public List<object> OptionSets { get; set; } = [];

            public int NumberOfOptions { get; set; }
            public int NumberOfRules { get; set; }
        }

        [LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.")]
        public class ProductVariantAttributeValueModel : EntityModelBase, ILocalizedModel<ProductVariantAttributeValueLocalizedModel>
        {
            public int ProductId { get; set; }
            public int ProductVariantAttributeId { get; set; }

            [LocalizedDisplay("*Alias")]
            public string Alias { get; set; }

            [LocalizedDisplay("*Name")]
            public string Name { get; set; }
            public string NameString { get; set; }

            [LocalizedDisplay("*ColorSquaresRgb")]
            [UIHint("Color")]
            public string Color { get; set; }
            public bool HasColor { get; set; }
            public bool IsListTypeAttribute { get; set; }

            [LocalizedDisplay("*Picture")]
            [UIHint("Media")]
            [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true), AdditionalMetadata("entityType", "ProductVariantAttributeValue")]
            public int PictureId { get; set; }

            [LocalizedDisplay("*PriceAdjustment")]
            public decimal PriceAdjustment { get; set; }

            [LocalizedDisplay("*PriceAdjustment")]
            public string PriceAdjustmentString { get; set; }

            [LocalizedDisplay("*WeightAdjustment")]
            public decimal WeightAdjustment { get; set; }

            [LocalizedDisplay("*WeightAdjustment")]
            public string WeightAdjustmentString { get; set; }

            [LocalizedDisplay("*IsPreSelected")]
            public bool IsPreSelected { get; set; }

            [LocalizedDisplay("Common.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [LocalizedDisplay("*ValueTypeId")]
            public int ValueTypeId { get; set; }

            [LocalizedDisplay("*ValueTypeId")]
            public string TypeName { get; set; }
            public string TypeNameClass { get; set; }

            [LocalizedDisplay("*LinkedProduct")]
            public int LinkedProductId { get; set; }

            [LocalizedDisplay("*LinkedProduct")]
            public string LinkedProductName { get; set; }
            public string LinkedProductTypeName { get; set; }
            public string LinkedProductTypeLabelHint { get; set; }
            public string LinkedProductEditUrl { get; set; }

            [LocalizedDisplay("*Quantity")]
            public int Quantity { get; set; }
            public string QuantityInfo { get; set; }

            public List<ProductVariantAttributeValueLocalizedModel> Locales { get; set; } = new();
        }

        [LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.")]
        public class ProductVariantAttributeValueLocalizedModel : ILocalizedLocaleModel
        {
            public int LanguageId { get; set; }

            [LocalizedDisplay("*Alias")]
            public string Alias { get; set; }

            [LocalizedDisplay("*Name")]
            public string Name { get; set; }
        }

        #endregion
    }

    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ProductLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*FullDescription")]
        public string FullDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [LocalizedDisplay("*BundleTitleText")]
        public string BundleTitleText { get; set; }
    }

    public class DownloadVersion
    {
        public int? DownloadId { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public string FileVersion { get; set; }
    }

    public partial class ProductModelValidator : SmartValidator<ProductModel>
    {
        public ProductModelValidator(SmartDbContext db, Localizer T)
        {
            ApplyEntityRules<Product>(db);
            //ApplyNonNullableValueTypeRules();

            RuleFor(x => x.TaxCategoryId)
                .NotNull()  // Nullable required for IsTaxExempt.
                .NotEqual(0)
                .When(x => !x.IsTaxExempt);

            When(x => x.IsTabLoaded("Inventory"), () =>
            {
                RuleFor(x => x.OrderMinimumQuantity).GreaterThan(0); // dont't remove "Admin.Validation.ValueGreaterZero" resource. It is used elsewhere.
                RuleFor(x => x.OrderMaximumQuantity).GreaterThan(0);
            });

            // validate PAnGV
            When(x => x.BasePriceEnabled && x.IsTabLoaded("Price"), () =>
            {
                RuleFor(x => x.Price).NotEmpty();

                RuleFor(x => x.BasePriceMeasureUnit).NotEmpty().WithMessage(T("Admin.Catalog.Products.Fields.BasePriceMeasureUnit.Required"));
                RuleFor(x => x.BasePriceBaseAmount)
                    .NotEmpty().WithMessage(T("Admin.Catalog.Products.Fields.BasePriceBaseAmount.Required"))
                    .GreaterThan(0).WithMessage(T("Admin.Catalog.Products.Fields.BasePriceBaseAmount.Required"));
                RuleFor(x => x.BasePriceAmount)
                    .NotEmpty().WithMessage(T("Admin.Catalog.Products.Fields.BasePriceAmount.Required"))
                    .GreaterThan(0).WithMessage(T("Admin.Catalog.Products.Fields.BasePriceAmount.Required"));
            });

            When(x => x.IsTabLoaded("Downloads"), () =>
            {
                RuleFor(x => x.DownloadFileVersion)
                    .NotEmpty()
                    .When(x => x.DownloadId != null && x.DownloadId != 0)
                    .WithMessage(T("Admin.Catalog.Products.Download.SemanticVersion.NotValid"));

                RuleFor(x => x.NewVersion)
                    .NotEmpty()
                    .When(x => x.NewVersionDownloadId != null && x.NewVersionDownloadId != 0)
                    .WithMessage(T("Admin.Catalog.Products.Download.SemanticVersion.NotValid"));
            });

            When(x => x.ProductTypeId == (int)ProductType.GroupedProduct, () =>
            {
                RuleFor(x => x.GroupedProductConfiguration.PageSize).GreaterThan(0);
            });
        }
    }

    public partial class ProductVariantAttributeValueModelValidator : SmartValidator<ProductModel.ProductVariantAttributeValueModel>
    {
        public ProductVariantAttributeValueModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1).When(x => x.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage);
        }
    }

    public partial class TierPriceModelValidator : SmartValidator<ProductModel.TierPriceModel>
    {
        public TierPriceModelValidator()
        {
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
        }
    }

    [Mapper(Lifetime = ServiceLifetime.Singleton)]
    public class ProductMapper : IMapper<Product, ProductModel>
    {
        public async Task MapAsync(Product from, ProductModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
        }
    }
}