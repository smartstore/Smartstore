using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductDetailsModelContext
    {
        public ProductDetailsModelContext()
        {
        }

        /// <summary>
        /// Applies the property references of another <see cref="ProductDetailsModelContext"/> instance.
        /// Only to be used for child items like associated products or bundle items,
        /// otherwise use <see cref="CatalogHelper.CreateModelContext"/>.
        /// </summary>
        public ProductDetailsModelContext(ProductDetailsModelContext other)
        {
            Product = other.Product;
            BatchContext = other.BatchContext;
            VariantQuery = other.VariantQuery;
            Customer = other.Customer;
            Store = other.Store;
            Currency = other.Currency;
            DisplayPrices = other.DisplayPrices;

            AssociatedProducts = other.AssociatedProducts;
            GroupedProductConfiguration = other.GroupedProductConfiguration;
        }

        public Product Product { get; set; }
        public ProductBatchContext BatchContext { get; set; }
        public ProductVariantQuery VariantQuery { get; set; }
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Currency Currency { get; set; }
        public bool DisplayPrices { get; set; }

        public bool IsAssociatedProduct { get; set; }
        public IList<Product> AssociatedProducts { get; set; }
        public GroupedProductConfiguration GroupedProductConfiguration { get; set; }

        public Product ParentProduct { get; set; }
        public ProductBundleItem ProductBundleItem { get; set; }

        /// <summary>
        /// The selected attributes based on <see cref="VariantQuery"/>. <c>null</c> if none have been selected (then the preselected attributes are used).
        /// </summary>
        public ProductVariantAttributeSelection SelectedAttributes { get; set; }
    }

    public partial class ProductDetailsModel : EntityModelBase
    {
        public MediaGalleryModel MediaGalleryModel { get; set; } = new();
        public MetaPropertiesModel MetaProperties { get; set; } = new();

        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> ShortDescription { get; set; }
        public LocalizedValue<string> FullDescription { get; set; }
        public string ProductTemplateViewPath { get; set; }
        public LocalizedValue<string> MetaKeywords { get; set; }
        public LocalizedValue<string> MetaDescription { get; set; }
        public LocalizedValue<string> MetaTitle { get; set; }
        public string SeName { get; set; }
        public string CanonicalUrl { get; set; }
        public string UpdateUrl { get; set; }
        public ProductType ProductType { get; set; }
        public bool VisibleIndividually { get; set; }

        public int PictureSize { get; set; }
        public int ThumbDimensions { get; set; }

        public ProductCondition Condition { get; set; }
        public bool ShowCondition { get; set; }
        public string LocalizedCondition { get; set; }

        public bool ShowSku { get; set; }
        public string Sku { get; set; }

        public bool ShowManufacturerPartNumber { get; set; }
        public string ManufacturerPartNumber { get; set; }

        public bool ShowGtin { get; set; }
        public string Gtin { get; set; }

        public bool HasSampleDownload { get; set; }

        public GiftCardModel GiftCard { get; set; } = new();
        public string GiftCardFieldPrefix => GiftCardQueryItem.CreateKey(Id, BundleItem.Id, null);

        public string StockAvailability { get; set; }
        public bool IsAvailable { get; set; } = true;

        public bool IsCurrentCustomerRegistered { get; set; }
        public bool DisplayBackInStockSubscription { get; set; }
        public bool BackInStockAlreadySubscribed { get; set; }

        public ProductDetailsPriceModel Price { get; set; } = new();
        public AddToCartModel AddToCart { get; set; } = new();
        public List<ProductVariantAttributeModel> ProductVariantAttributes { get; set; } = [];
        public string AttributeInfo { get; set; }

        public bool DisplayAdminLink { get; set; }
        public bool ShowLegalInfo { get; set; }
        public string LegalInfo { get; set; }

        public bool ShowWeight { get; set; }
        public bool ShowDimensions { get; set; }
        public string DimensionSystemKeyword { get; set; }
        public string Weight { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public decimal WeightValue { get; set; }
        public decimal LengthValue { get; set; }
        public decimal WidthValue { get; set; }
        public decimal HeightValue { get; set; }

        public LocalizedValue<string> QuantityUnitName { get; set; }
        public LocalizedValue<string> QuantityUnitNamePlural { get; set; }
        public bool DisplayProductReviews { get; set; }
        public bool IsBasePriceEnabled { get; set; }
        public string BasePriceInfo { get; set; }
        public string BundleTitleText { get; set; }
        public bool BundlePerItemShipping { get; set; }
        public bool BundlePerItemPricing { get; set; }
        public bool BundlePerItemShoppingCart { get; set; }

        public bool IsShippingEnabled { get; set; }
        public DeliveryTimesPresentation DeliveryTimesPresentation { get; set; }
        public bool DisplayDeliveryTimeAccordingToStock { get; set; }
        public string DeliveryTimeName { get; set; }
        public string DeliveryTimeHexValue { get; set; }
        public string DeliveryTimeDate { get; set; }

        public ProductVariantAttributeCombination SelectedCombination { get; set; }

        public List<BrandOverviewModel> Brands { get; set; } = [];
        public int ReviewCount { get; set; }
        public ProductReviewOverviewModel ReviewOverview { get; set; } = new();

        public GroupedProductModel GroupedProduct { get; set; }
        public bool IsAssociatedProduct { get; set; }

        public List<ProductDetailsModel> BundledItems { get; set; } = [];
        public ProductBundleItemModel BundleItem { get; set; } = new();
        public bool IsBundlePart { get; set; }

        public List<ProductSpecificationModel> SpecificationAttributes { get; set; } = [];

        public List<ProductTagModel> ProductTags { get; set; } = [];
        public bool ShowProductTags { get; set; }

        public ProductReviewsModel ProductReviews { get; set; } = new();
        public ProductSummaryModel AlsoPurchased { get; set; }
        public ProductSummaryModel RelatedProducts { get; set; }

        public bool CompareEnabled { get; set; }
        public bool TellAFriendEnabled { get; set; }
        public bool AskQuestionEnabled { get; set; }
        public string HotlineTelephoneNumber { get; set; }
        public string ProductShareCode { get; set; }

        public Dictionary<string, ActionItemModel> ActionItems { get; set; } = [];

        #region Nested Classes

        public partial class ActionItemModel : ModelBase
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public string Tooltip { get; set; }
            public string Href { get; set; }
            public string Target { get; set; }
            public string CssClass { get; set; }
            public string IconCssClass { get; set; }
            public bool IsPrimary { get; set; }
            public string PrimaryActionColor { get; set; }
            public int Priority { get; set; }
        }

        public partial class AddToCartModel : ModelBase, IQuantityInput
        {
            public int ProductId { get; set; }

            [LocalizedDisplay("Products.Qty")]
            public int EnteredQuantity { get; set; }

            [LocalizedDisplay("Products.EnterProductPrice")]
            public bool CustomerEntersPrice { get; set; }

            [LocalizedDisplay("Products.EnterProductPrice")]
            public decimal CustomerEnteredPrice { get; set; }
            public string CustomerEnteredPriceRange { get; set; }

            public int MinOrderAmount { get; set; }
            public int MaxOrderAmount { get; set; }
            public LocalizedValue<string> QuantityUnitName { get; set; }
            public LocalizedValue<string> QuantityUnitNamePlural { get; set; }
            public int QuantityStep { get; set; }
            public int? MaxInStock { get; set; }
            public bool HideQuantityControl { get; set; }
            public QuantityControlType QuantityControlType { get; set; }
            public bool CollapsibleAssociatedProduct { get; set; }

            public bool DisableBuyButton { get; set; }
            public bool DisableWishlistButton { get; set; }
            public List<SelectListItem> AllowedQuantities { get; set; } = [];
            public bool AvailableForPreOrder { get; set; }
        }

        [LocalizedDisplay("Products.GiftCard.")]
        public partial class GiftCardModel : ModelBase
        {
            public bool IsGiftCard { get; set; }

            [LocalizedDisplay("*RecipientName")]
            public string RecipientName { get; set; }

            [LocalizedDisplay("*RecipientEmail")]
            public string RecipientEmail { get; set; }

            [LocalizedDisplay("*SenderName")]
            public string SenderName { get; set; }

            [LocalizedDisplay("*SenderEmail")]
            public string SenderEmail { get; set; }

            [LocalizedDisplay("*Message")]
            [SanitizeHtml]
            public string Message { get; set; }

            public GiftCardType GiftCardType { get; set; }
        }

        public partial class ProductVariantAttributeModel : ChoiceModel
        {
            public int ProductId { get; set; }
            public int BundleItemId { get; set; }
            public int ProductAttributeId { get; set; }
            public ProductVariantAttribute ProductAttribute { get; set; }

            public override string BuildControlId()
                => ProductVariantQueryItem.CreateKey(ProductId, BundleItemId, ProductAttributeId, Id);

            public override string GetFileUploadUrl(IUrlHelper url)
                => url.Action("UploadFileProductAttribute", "ShoppingCart", new { productId = ProductId, productAttributeId = ProductAttributeId });

            public bool ShouldBeRendered
                => IsActive && (Values.Count > 0 || !(ProductAttribute?.IsListTypeAttribute() ?? false));
        }

        public partial class ProductVariantAttributeValueModel : ChoiceItemModel
        {
            public ProductVariantAttributeValue ProductAttributeValue { get; set; }

            public override string GetItemLabel()
            {
                var label = Name;

                if (QuantityInfo > 1)
                {
                    label = "{0} x {1}".FormatCurrentUI(QuantityInfo, label);
                }

                if (PriceAdjustment.HasValue())
                {
                    label += PriceAdjustment;
                }

                return label;
            }
        }

        public partial class ProductBundleItemModel : EntityModelBase
        {
            public int Quantity { get; set; }
            public bool HideThumbnail { get; set; }
            public bool Visible { get; set; }
            public bool IsBundleItemPricing { get; set; }
        }

        #endregion
    }
}
