using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Catalog.")]
    public class CatalogSettingsModel : ILocalizedModel<PriceSettingsLocalizedModel>
    {
        #region General

        [LocalizedDisplay("*ShowProductSku")]
        public bool ShowProductSku { get; set; }

        [LocalizedDisplay("*ShowManufacturerPartNumber")]
        public bool ShowManufacturerPartNumber { get; set; }

        [LocalizedDisplay("*ShowGtin")]
        public bool ShowGtin { get; set; }

        [LocalizedDisplay("*ShowWeight")]
        public bool ShowWeight { get; set; }

        [LocalizedDisplay("*ShowDimensions")]
        public bool ShowDimensions { get; set; }

        [LocalizedDisplay("*IgnoreFeaturedProducts")]
        public bool IgnoreFeaturedProducts { get; set; }

        [LocalizedDisplay("*CompareProductsEnabled")]
        public bool CompareProductsEnabled { get; set; }

        [LocalizedDisplay("*IncludeShortDescriptionInCompareProducts")]
        public bool IncludeShortDescriptionInCompareProducts { get; set; }

        [LocalizedDisplay("*IncludeFullDescriptionInCompareProducts")]
        public bool IncludeFullDescriptionInCompareProducts { get; set; }

        [LocalizedDisplay("*UseSmallProductBoxOnHomePage")]
        public bool UseSmallProductBoxOnHomePage { get; set; }

        [LocalizedDisplay("*ShowBestsellersOnHomepage")]
        public bool ShowBestsellersOnHomepage { get; set; }

        [LocalizedDisplay("*NumberOfBestsellersOnHomepage")]
        public int NumberOfBestsellersOnHomepage { get; set; }

        [LocalizedDisplay("*EnableHtmlTextCollapser")]
        public bool EnableHtmlTextCollapser { get; set; }

        [LocalizedDisplay("*HtmlTextCollapsedHeight")]
        public int HtmlTextCollapsedHeight { get; set; }

        [LocalizedDisplay("*ShowDefaultQuantityUnit")]
        public bool ShowDefaultQuantityUnit { get; set; }

        [LocalizedDisplay("*ShowDefaultDeliveryTime")]
        public bool ShowDefaultDeliveryTime { get; set; }

        [LocalizedDisplay("*ShowPopularProductTagsOnHomepage")]
        public bool ShowPopularProductTagsOnHomepage { get; set; }

        [LocalizedDisplay("*ShowManufacturersOnHomepage")]
        public bool ShowManufacturersOnHomepage { get; set; }

        [LocalizedDisplay("*ShowManufacturersInOffCanvas")]
        public bool ShowManufacturersInOffCanvas { get; set; }

        [LocalizedDisplay("*MaxItemsToDisplayInCatalogMenu")]
        public int MaxItemsToDisplayInCatalogMenu { get; set; }

        [LocalizedDisplay("*ManufacturerItemsToDisplayOnHomepage")]
        public int ManufacturerItemsToDisplayOnHomepage { get; set; }

        [LocalizedDisplay("*ManufacturerItemsToDisplayInOffcanvasMenu")]
        public int ManufacturerItemsToDisplayInOffcanvasMenu { get; set; }

        [LocalizedDisplay("*ShowManufacturerPictures")]
        public bool ShowManufacturerPictures { get; set; }

        [LocalizedDisplay("*HideManufacturerDefaultPictures")]
        public bool HideManufacturerDefaultPictures { get; set; }

        [LocalizedDisplay("*SortManufacturersAlphabetically")]
        public bool SortManufacturersAlphabetically { get; set; }

        [LocalizedDisplay("*HideCategoryDefaultPictures")]
        public bool HideCategoryDefaultPictures { get; set; }

        [LocalizedDisplay("*HideProductDefaultPictures")]
        public bool HideProductDefaultPictures { get; set; }

        [LocalizedDisplay("*ShowProductCondition")]
        public bool ShowProductCondition { get; set; }

        #endregion

        #region Product lists

        #region Navigation

        [LocalizedDisplay("*ShowProductsFromSubcategories")]
        public bool ShowProductsFromSubcategories { get; set; }

        [LocalizedDisplay("*IncludeFeaturedProductsInNormalLists")]
        public bool IncludeFeaturedProductsInNormalLists { get; set; }

        [LocalizedDisplay("*ShowCategoryProductNumber")]
        public bool ShowCategoryProductNumber { get; set; }

        [LocalizedDisplay("*ShowCategoryProductNumberIncludingSubcategories")]
        public bool ShowCategoryProductNumberIncludingSubcategories { get; set; }

        [LocalizedDisplay("*CategoryBreadcrumbEnabled")]
        public bool CategoryBreadcrumbEnabled { get; set; }

        [LocalizedDisplay("*SubCategoryDisplayType")]
        public SubCategoryDisplayType SubCategoryDisplayType { get; set; }

        #endregion

        #region Product list

        [LocalizedDisplay("*AllowProductSorting")]
        public bool AllowProductSorting { get; set; }

        [LocalizedDisplay("*DefaultViewMode")]
        public string DefaultViewMode { get; set; }

        [LocalizedDisplay("*DefaultSortOrderMode")]
        public ProductSortingEnum DefaultSortOrder { get; set; }

        [LocalizedDisplay("*AllowProductViewModeChanging")]
        public bool AllowProductViewModeChanging { get; set; }

        [LocalizedDisplay("*DefaultProductListPageSize")]
        public int DefaultProductListPageSize { get; set; }

        [LocalizedDisplay("*DefaultPageSizeOptions")]
        public string DefaultPageSizeOptions { get; set; }

        [LocalizedDisplay("*GridStyleListColumnSpan")]
        public GridColumnSpan GridStyleListColumnSpan { get; set; }

        [LocalizedDisplay("*ShowSubCategoriesInSubPages")]
        public bool ShowSubCategoriesInSubPages { get; set; }

        [LocalizedDisplay("*ShowDescriptionInSubPages")]
        public bool ShowDescriptionInSubPages { get; set; }

        [LocalizedDisplay("*IncludeFeaturedProductsInSubPages")]
        public bool IncludeFeaturedProductsInSubPages { get; set; }

        #endregion

        #region Products

        [LocalizedDisplay("*ShowShortDescriptionInGridStyleLists")]
        public bool ShowShortDescriptionInGridStyleLists { get; set; }

        [LocalizedDisplay("*ShowManufacturerInGridStyleLists")]
        public bool ShowManufacturerInGridStyleLists { get; set; }

        [LocalizedDisplay("*ShowManufacturerLogoInLists")]
        public bool ShowManufacturerLogoInLists { get; set; }

        [LocalizedDisplay("*LinkManufacturerLogoInLists")]
        public bool LinkManufacturerLogoInLists { get; set; }

        [LocalizedDisplay("*ShowProductOptionsInLists")]
        public bool ShowProductOptionsInLists { get; set; }

        [LocalizedDisplay("*DeliveryTimesInLists")]
        public DeliveryTimesPresentation DeliveryTimesInLists { get; set; }

        [LocalizedDisplay("*ShowColorSquaresInLists")]
        public bool ShowColorSquaresInLists { get; set; }

        [LocalizedDisplay("*HideBuyButtonInLists")]
        public bool HideBuyButtonInLists { get; set; }

        [LocalizedDisplay("*LabelAsNewForMaxDays")]
        public int? LabelAsNewForMaxDays { get; set; }

        #endregion

        #region Product tags

        [LocalizedDisplay("*NumberOfProductTags")]
        public int NumberOfProductTags { get; set; }

        #endregion

        #endregion

        #region Customers 

        [LocalizedDisplay("*ShowProductReviewsInProductLists")]
        public bool ShowProductReviewsInProductLists { get; set; }

        [LocalizedDisplay("*ShowProductReviewsInProductDetail")]
        public bool ShowProductReviewsInProductDetail { get; set; }

        [LocalizedDisplay("*ShowVerfiedPurchaseBadge")]
        public bool ShowVerfiedPurchaseBadge { get; set; } = true;

        [LocalizedDisplay("*ProductReviewsMustBeApproved")]
        public bool ProductReviewsMustBeApproved { get; set; }

        [LocalizedDisplay("*AllowAnonymousUsersToReviewProduct")]
        public bool AllowAnonymousUsersToReviewProduct { get; set; }

        [LocalizedDisplay("*NotifyStoreOwnerAboutNewProductReviews")]
        public bool NotifyStoreOwnerAboutNewProductReviews { get; set; }

        [LocalizedDisplay("*EmailAFriendEnabled")]
        public bool EmailAFriendEnabled { get; set; }

        [LocalizedDisplay("*AskQuestionEnabled")]
        public bool AskQuestionEnabled { get; set; }

        [LocalizedDisplay("*AllowAnonymousUsersToEmailAFriend")]
        public bool AllowAnonymousUsersToEmailAFriend { get; set; }

        [LocalizedDisplay("*AllowDifferingEmailAddressForEmailAFriend")]
        public bool AllowDifferingEmailAddressForEmailAFriend { get; set; }

        #endregion

        #region Product detail

        [LocalizedDisplay("*RecentlyViewedProductsEnabled")]
        public bool RecentlyViewedProductsEnabled { get; set; }

        [LocalizedDisplay("*RecentlyViewedProductsNumber")]
        public int RecentlyViewedProductsNumber { get; set; }

        [LocalizedDisplay("*RecentlyAddedProductsEnabled")]
        public bool RecentlyAddedProductsEnabled { get; set; }

        [LocalizedDisplay("*RecentlyAddedProductsNumber")]
        public int RecentlyAddedProductsNumber { get; set; }

        [LocalizedDisplay("*ProductsAlsoPurchasedEnabled")]
        public bool ProductsAlsoPurchasedEnabled { get; set; }

        [LocalizedDisplay("*ProductsAlsoPurchasedNumber")]
        public int ProductsAlsoPurchasedNumber { get; set; }

        [LocalizedDisplay("*DisplayAllImagesNumber")]
        public int DisplayAllImagesNumber { get; set; }

        [LocalizedDisplay("*ShowManufacturerInProductDetail")]
        public bool ShowManufacturerInProductDetail { get; set; }

        [LocalizedDisplay("*ShowManufacturerPicturesInProductDetail")]
        public bool ShowManufacturerPicturesInProductDetail { get; set; }

        [LocalizedDisplay("*DeliveryTimesInProductDetail")]
        public DeliveryTimesPresentation DeliveryTimesInProductDetail { get; set; }

        [UIHint("DeliveryTimes")]
        [LocalizedDisplay("*DeliveryTimeIdForEmptyStock")]
        public int? DeliveryTimeIdForEmptyStock { get; set; }

        [LocalizedDisplay("*ShowLinkedAttributeValueQuantity")]
        public bool ShowLinkedAttributeValueQuantity { get; set; }

        [LocalizedDisplay("*ShowLinkedAttributeValueImage")]
        public bool ShowLinkedAttributeValueImage { get; set; }

        [LocalizedDisplay("*ShowProductTags")]
        public bool ShowProductTags { get; set; }

        #endregion

        public PriceSettingsModel PriceSettings { get; set; } = new();
        public List<PriceSettingsLocalizedModel> Locales { get; set; } = new();
    }

    public partial class CatalogSettingsValidator : SettingModelValidator<CatalogSettingsModel, CatalogSettings>
    {
        public CatalogSettingsValidator()
        {
            RuleFor(x => x.PriceSettings.ShowOfferCountdownRemainingHours)
                .GreaterThan(0)
                .WhenSettingOverriden((m, ctx) => m.PriceSettings.ShowOfferCountdownRemainingHours != null);

            RuleFor(x => x.MaxItemsToDisplayInCatalogMenu)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.HtmlTextCollapsedHeight)
                .GreaterThan(0);

            RuleFor(x => x.ManufacturerItemsToDisplayOnHomepage)
                .GreaterThan(0);

            RuleFor(x => x.NumberOfBestsellersOnHomepage)
                .GreaterThan(0);

            RuleFor(x => x.DefaultProductListPageSize)
                .GreaterThan(0);

            RuleFor(x => x.NumberOfProductTags)
                .GreaterThan(0);

            RuleFor(x => x.RecentlyViewedProductsNumber)
                .GreaterThan(0);

            RuleFor(x => x.RecentlyAddedProductsNumber)
                .GreaterThan(0);

            RuleFor(x => x.LabelAsNewForMaxDays)
                .GreaterThan(0)
                .WhenSettingOverriden((m, ctx) => m.LabelAsNewForMaxDays != null);

            RuleFor(x => x.DisplayAllImagesNumber).GreaterThanOrEqualTo(0);
        }
    }
}
