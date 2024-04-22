using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Catalog
{
    public enum SubCategoryDisplayType
    {
        Hide = 0,
        AboveProductList = 5,
        Bottom = 10
    }

    public enum GridColumnSpan
    {
        Max2Cols = 2,
        Max3Cols = 3,
        Max4Cols = 4,
        Max5Cols = 5,
        Max6Cols = 6
    }

    public class CatalogSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to display product SKU
        /// </summary>
        public bool ShowProductSku { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display manufacturer part number of a product
        /// </summary>
        public bool ShowManufacturerPartNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display GTIN of a product
        /// </summary>
        public bool ShowGtin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display weight of a product
        /// </summary>
        public bool ShowWeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display dimensions of a product
        /// </summary>
        public bool ShowDimensions { get; set; }

        /// <summary>
        /// Specifies the presentation of delivery times in product lists.
        /// </summary>
        public DeliveryTimesPresentation DeliveryTimesInLists { get; set; } = DeliveryTimesPresentation.DateOnly;

        /// <summary>
        /// Specifies the presentation of delivery times in product detail pages.
        /// </summary>
        public DeliveryTimesPresentation DeliveryTimesInProductDetail { get; set; } = DeliveryTimesPresentation.DateOnly;

        /// <summary>
        /// Gets or sets a value indicating whether to display quantity of linked product at attribute values
        /// </summary>
        public bool ShowLinkedAttributeValueQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display the image of linked product at attribute values
        /// </summary>
        public bool ShowLinkedAttributeValueImage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to display product tags on the product detail page.
        /// </summary>
        public bool ShowProductTags { get; set; } = true;

        /// <summary>
		/// Gets or sets a value indicating how many menu items will be displayed
		/// </summary>
        public int? MaxItemsToDisplayInCatalogMenu { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether product sorting is enabled
        /// </summary>
        public bool AllowProductSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets the default sort order in product lists
        /// </summary>
        public ProductSortingEnum DefaultSortOrder { get; set; } = ProductSortingEnum.Relevance;

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to change product view mode
        /// </summary>
        public bool AllowProductViewModeChanging { get; set; } = true;

        /// <summary>
        /// Gets or sets the default view mode for product lists
        /// </summary>
        public string DefaultViewMode { get; set; } = "grid";

        /// <summary>
        /// Gets or sets a value indicating whether a category details page should include products from subcategories
        /// </summary>
        public bool ShowProductsFromSubcategories { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether number of products should be displayed beside each category
        /// </summary>
        public bool ShowCategoryProductNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether we include subcategories (when 'ShowCategoryProductNumber' is 'true')
        /// </summary>
        public bool ShowCategoryProductNumberIncludingSubcategories { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether category breadcrumb is enabled
        /// </summary>
        public bool CategoryBreadcrumbEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether and where to display a list of subcategories
        /// </summary>
        public SubCategoryDisplayType SubCategoryDisplayType { get; set; } = SubCategoryDisplayType.AboveProductList;

        /// <summary>
        /// An option indicating whether sub pages should display the subcategories
        /// </summary>
        public bool ShowSubCategoriesInSubPages { get; set; }

        /// <summary>
        /// An option indicating whether sub pages should display the category/manufacturer description
        /// </summary>
        public bool ShowDescriptionInSubPages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display reviews in product lists
        /// </summary>
        public bool ShowProductReviewsInProductLists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display reviews in product detail
        /// </summary>
        public bool ShowProductReviewsInProductDetail { get; set; } = true;

        /// <summary>
        /// Gets or sets a value whether to display the badge for verified purchases.
        /// </summary>
        public bool ShowVerfiedPurchaseBadge { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating product reviews must be approved
        /// </summary>
        public bool ProductReviewsMustBeApproved { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default rating value of the product reviews
        /// </summary>
        public int DefaultProductRatingValue { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether to allow anonymous users write product reviews.
        /// </summary>
        public bool AllowAnonymousUsersToReviewProduct { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether notification of a store owner about new product reviews is enabled
        /// </summary>
        public bool NotifyStoreOwnerAboutNewProductReviews { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether product 'Email a friend' feature is enabled
        /// </summary>
        public bool EmailAFriendEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether 'ask product question' feature is enabled
        /// </summary>
        public bool AskQuestionEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to enter a differing email address 
        /// </summary>
        public bool AllowDifferingEmailAddressForEmailAFriend { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to allow anonymous users to email a friend.
        /// </summary>
        public bool AllowAnonymousUsersToEmailAFriend { get; set; } = false;

        /// <summary>
        /// Gets or sets a number of "Recently viewed products"
        /// </summary>
        public int RecentlyViewedProductsNumber { get; set; } = 8;

        /// <summary>
        /// Gets or sets a value indicating whether "Recently viewed products" feature is enabled
        /// </summary>
        public bool RecentlyViewedProductsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a number of "Recently added products"
        /// </summary>
        public int RecentlyAddedProductsNumber { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether "Recently added products" feature is enabled
        /// </summary>
        public bool RecentlyAddedProductsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether "Compare products" feature is enabled
        /// </summary>
        public bool CompareProductsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show bestsellers on home page
        /// </summary>
        public bool ShowBestsellersOnHomepage { get; set; }

        /// <summary>
        /// Gets or sets a number of bestsellers on home page
        /// </summary>
        public int NumberOfBestsellersOnHomepage { get; set; } = 12;

        /// <summary>
        /// Gets or sets a value indicating whether to show manufacturers on home page
        /// </summary>
        public bool ShowManufacturersOnHomepage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show manufacturers in offcanvas menu
        /// </summary>
        public bool ShowManufacturersInOffCanvas { get; set; } = true;

        /// <summary>
        /// Gets or sets the value indicating how many manufacturers on home page
        /// </summary>
        public int ManufacturerItemsToDisplayOnHomepage { get; set; } = 18;

        /// <summary>
        /// Gets or sets the value indicating how many manufacturers in offcanvas menu
        /// </summary>
        public int ManufacturerItemsToDisplayInOffcanvasMenu { get; set; } = 20;

        /// <summary>
        /// Gets or sets a value indicating whether to show manufacturer pictures or names on home page
        /// </summary>
        public bool ShowManufacturerPictures { get; set; } = true;

        /// <summary>
		/// Gets or sets a value indicating whether to display manufacturer detail links in product detail pages
		/// </summary>
		public bool ShowManufacturerInProductDetail { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to display pictures or textual links to manufacturer pages in product detail pages
        /// </summary>
        public bool ShowManufacturerPicturesInProductDetail { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to hide manufacturer default pictures
        /// </summary>
        public bool HideManufacturerDefaultPictures { get; set; }

        /// <summary>
		/// Gets or sets a value indicating whether to hide manufacturer default pictures
		/// </summary>
		public bool SortManufacturersAlphabetically { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide category default pictures
        /// </summary>
        public bool HideCategoryDefaultPictures { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide product default pictures
        /// </summary>
        public bool HideProductDefaultPictures { get; set; }

        /// <summary>
        /// Gets or sets a value whether to display the product condition.
        /// </summary>
        public bool ShowProductCondition { get; set; }

        /// <summary>
        /// Gets or sets "List of products purchased by other customers who purchased the above" option is enable
        /// </summary>
        public bool ProductsAlsoPurchasedEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a number of products also purchased by other customers to display
        /// </summary>
        public int ProductsAlsoPurchasedNumber { get; set; } = 12;

        /// <summary>
        /// Gets or sets a number of product tags that appear in the tag cloud
        /// </summary>
        public int NumberOfProductTags { get; set; } = 15;

        /// <summary>
        /// Gets or sets a number of products per page on a product list page
        /// </summary>
        public int DefaultProductListPageSize { get; set; } = 24;

        /// <summary>
        /// Gets or sets a value indicating whether customers can select page size in product listings
        /// </summary>
        public bool AllowCustomersToSelectPageSize { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold above which only images that are not assigned to any or the selected attribute combination are displayed.
        /// </summary>
        public int DisplayAllImagesNumber { get; set; } = 6;

        public bool ShowManufacturerInGridStyleLists { get; set; } = true;

        /// <summary>
        /// Whether to show brand logo instead of textual name in product lists
        /// </summary>
        public bool ShowManufacturerLogoInLists { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the manufacturer logo should be linked in product lists.
        /// </summary>
        public bool LinkManufacturerLogoInLists { get; set; } = true;

        public bool ShowShortDescriptionInGridStyleLists { get; set; } = true;

        public bool ShowProductOptionsInLists { get; set; } = true;

        public bool ShowColorSquaresInLists { get; set; } = true;

        public bool HideBuyButtonInLists { get; set; }

        public int? LabelAsNewForMaxDays { get; set; }

        public bool ShowDefaultQuantityUnit { get; set; }

        public bool ShowDefaultDeliveryTime { get; set; }

        public bool ShowPopularProductTagsOnHomepage { get; set; } = false;

        /// <summary>
        /// Gets or sets the available customer selectable default page size options
        /// </summary>
        public string DefaultPageSizeOptions { get; set; } = "12,24,36,48,72,120";

        /// <summary>
        /// Gets or sets a value indicating whether to include "Short description" in compare products
        /// </summary>
        public bool IncludeShortDescriptionInCompareProducts { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include "Full description" in compare products
        /// </summary>
        public bool IncludeFullDescriptionInCompareProducts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use small product boxes on home page
        /// </summary>
        public bool UseSmallProductBoxOnHomePage { get; set; } = true;

        /// <summary>
        /// An option indicating whether products on category and manufacturer pages should include featured products as well
        /// </summary>
        public bool IncludeFeaturedProductsInNormalLists { get; set; }

        /// <summary>
        /// An option indicating whether products on category and manufacturer pages should include featured products in sub pages as well
        /// </summary>
        public bool IncludeFeaturedProductsInSubPages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore featured products (side-wide)
        /// </summary>
        public bool IgnoreFeaturedProducts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating maximum number of 'back in stock' subscription per customer.
        /// </summary>
        public int MaximumBackInStockSubscriptions { get; set; } = 200;

        /// <summary>
        /// Gets or sets a maximum file upload size in bytes for product attributes ('File Upload' type)
        /// </summary>
        public int FileUploadMaximumSizeBytes { get; set; } = 1024 * 200; //200KB

        /// <summary>
        /// Gets or sets a list of allowed file extensions for customer uploaded files
        /// </summary>
        public List<string> FileUploadAllowedExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating if html long text should be collapsed
        /// </summary>
        public bool EnableHtmlTextCollapser { get; set; } = true;

        /// <summary>
        /// Gets or sets the height of collapsed text
        /// </summary>
        public int HtmlTextCollapsedHeight { get; set; } = 260;

        /// <summary>
        /// Gets or sets the identifier of a delivery time displayed when the stock is empty.
        /// </summary>
        public int? DeliveryTimeIdForEmptyStock { get; set; }

        /// <summary>
        /// Gets or sets how many items to display maximally in the most recently used category list
        /// </summary>
        public int MostRecentlyUsedCategoriesMaxSize { get; set; } = 6;

        /// <summary>
        /// Gets or sets how many items to display maximally in the most recently used manufacturer list
        /// </summary>
        public int MostRecentlyUsedManufacturersMaxSize { get; set; } = 4;

        /// <summary>
        /// Gets or sets how many columns per row should be displayed at most in grid style lists on largest screen resolution.
        /// </summary>
        public GridColumnSpan GridStyleListColumnSpan { get; set; } = GridColumnSpan.Max4Cols;
    }
}