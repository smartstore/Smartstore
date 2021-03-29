using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling;
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

        public ProductDetailsModelContext(ProductDetailsModelContext other)
        {
            Product = other.Product;
            AssociatedProducts = other.AssociatedProducts;
            BundleItemDatas = other.BundleItemDatas;
            BatchContext = other.BatchContext;
            VariantQuery = other.VariantQuery;
            Customer = other.Customer;
            Store = other.Store;
            Currency = other.Currency;
        }

        public Product Product { get; set; }
        public IList<Product> AssociatedProducts { get; set; }
        public IList<ProductBundleItemData> BundleItemDatas { get; set; }
        public ProductBatchContext BatchContext { get; set; }
        public ProductVariantQuery VariantQuery { get; set; }
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Currency Currency { get; set; }

        public bool IsAssociatedProduct { get; set; }
        public ProductBundleItemData ProductBundleItem { get; set; }
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
        public ProductType ProductType { get; set; }
        public bool VisibleIndividually { get; set; }

        public int PictureSize { get; set; }
        
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

        public ProductPriceModel ProductPrice { get; set; } = new();
        public AddToCartModel AddToCart { get; set; } = new();
        public List<ProductVariantAttributeModel> ProductVariantAttributes { get; set; } = new();
        public string AttributeInfo { get; set; }

        public bool DisplayAdminLink { get; set; }
        public bool ShowLegalInfo { get; set; }
        public string LegalInfo { get; set; }
        public bool ShowWeight { get; set; }
        public bool ShowDimensions { get; set; }
        public decimal WeightValue { get; set; }
        public string Weight { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public int ThumbDimensions { get; set; }
        public LocalizedValue<string> QuantityUnitName { get; set; }
        public bool DisplayProductReviews { get; set; }
        public bool IsBasePriceEnabled { get; set; }
        public string BasePriceInfo { get; set; }
        public string BundleTitleText { get; set; }
        public bool BundlePerItemShipping { get; set; }
        public bool BundlePerItemPricing { get; set; }
        public bool BundlePerItemShoppingCart { get; set; }
        public bool DisplayTextForZeroPrices { get; set; }
        public PriceDisplayStyle PriceDisplayStyle { get; set; }

        public bool IsShippingEnabled { get; set; }
        public DeliveryTimesPresentation DeliveryTimesPresentation { get; set; }
        public bool DisplayDeliveryTimeAccordingToStock { get; set; }
        public string DeliveryTimeName { get; set; }
        public string DeliveryTimeHexValue { get; set; }
        public string DeliveryTimeDate { get; set; }

        public ProductVariantAttributeCombination SelectedCombination { get; set; }

        public List<BrandOverviewModel> Brands { get; set; } = new();
        public int ReviewCount { get; set; }
        public ProductReviewOverviewModel ReviewOverview { get; set; } = new();
        

        // A list of associated products. For example, "Grouped" products could have several child "simple" products
        public List<ProductDetailsModel> AssociatedProducts { get; set; } = new();
        public bool IsAssociatedProduct { get; set; }

        public List<ProductDetailsModel> BundledItems { get; set; } = new();
        public ProductBundleItemModel BundleItem { get; set; } = new();
        public bool IsBundlePart { get; set; }

        public List<ProductSpecificationModel> SpecificationAttributes { get; set; } = new();

        public List<ProductTagModel> ProductTags { get; set; } = new();
        
        public ProductReviewsModel ProductReviews { get; set; } = new();

        public ProductSummaryModel RelatedProducts { get; set; }

        public bool CompareEnabled { get; set; }
        public bool TellAFriendEnabled { get; set; }
        public bool AskQuestionEnabled { get; set; }
        public string HotlineTelephoneNumber { get; set; }
        public string ProductShareCode { get; set; }

        public Dictionary<string, ActionItemModel> ActionItems { get; set; } = new();

        #region Nested Classes

        public partial class ActionItemModel : ModelBase
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public string Tooltip { get; set; }
            public string Href { get; set; }
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
            public int QuantityStep { get; set; }
            public bool HideQuantityControl { get; set; }
            public QuantityControlType QuantiyControlType { get; set; }

            public bool DisableBuyButton { get; set; }
            public bool DisableWishlistButton { get; set; }
            public List<SelectListItem> AllowedQuantities { get; set; } = new();
            public bool AvailableForPreOrder { get; set; }
        }

        public partial class ProductPriceModel : ModelBase
        {
            public Money OldPrice { get; set; }

            public Money Price { get; set; }
            public Money PriceWithDiscount { get; set; }

            public float SavingPercent { get; set; }
            public Money SavingAmount { get; set; }

            public bool CustomerEntersPrice { get; set; }
            public bool CallForPrice { get; set; }

            public int ProductId { get; set; }

            public bool HidePrices { get; set; }
            public bool ShowLoginNote { get; set; }

            public bool DynamicPriceUpdate { get; set; }
            public bool BundleItemShowBasePrice { get; set; }

            public string NoteWithDiscount { get; set; }
            public string NoteWithoutDiscount { get; set; }

            public string PriceValidUntilUtc { get; set; }
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

        public partial class TierPriceModel : ModelBase
        {
            public Money Price { get; set; }
            public int Quantity { get; set; }
        }

        public partial class ProductVariantAttributeModel : ChoiceModel
        {
            public int ProductId { get; set; }
            public int BundleItemId { get; set; }
            public int ProductAttributeId { get; set; }
            public ProductVariantAttribute ProductAttribute { get; set; }

            public override string BuildControlId()
            {
                return ProductVariantQueryItem.CreateKey(ProductId, BundleItemId, ProductAttributeId, Id);
            }

            public override string GetFileUploadUrl(IUrlHelper url)
            {
                return url.Action("UploadFileProductAttribute", "ShoppingCart", new { productId = ProductId, productAttributeId = ProductAttributeId });
            }
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
                    label += " ({0})".FormatWith(PriceAdjustment);
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

    // TODO: (mh) (core) This doesn't belong here. Find a proper place.
    public static class ProductDetailsExtensions
    {
        public static bool ShouldBeRendered(this ProductDetailsModel.ProductVariantAttributeModel variantAttribute)
        {
            switch (variantAttribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.Checkboxes:
                case AttributeControlType.Boxes:
                    return variantAttribute.Values.Count > 0;
                default:
                    return true;
            }
        }

        public static bool ShouldBeRendered(this IEnumerable<ProductDetailsModel.ProductVariantAttributeModel> variantAttributes)
        {
            return variantAttributes?.FirstOrDefault(x => x.ShouldBeRendered()) != null;
        }
    }
}
