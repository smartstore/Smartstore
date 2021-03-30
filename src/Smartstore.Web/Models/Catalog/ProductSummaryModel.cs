using System;
using System.Collections.Generic;
using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductSummaryModel : ModelBase, IListActions, IDisposable
    {
        public readonly static ProductSummaryModel Empty = new(new PagedList<Product>(new List<Product>(), 0, int.MaxValue));

        public ProductSummaryModel(IPagedList<Product> products, CatalogSearchResult sourceResult = null)
        {
            Guard.NotNull(products, nameof(products));

            PagedList = products;
            SourceResult = sourceResult;
        }

        public CatalogSearchResult SourceResult { get; init; }
        public int? ThumbSize { get; set; }
        public bool ShowSku { get; set; }
        public bool ShowWeight { get; set; }
        public bool ShowDescription { get; set; }
        public bool ShowFullDescription { get; set; }
        public bool ShowBrand { get; set; }
        public bool ShowDimensions { get; set; }
        public bool ShowLegalInfo { get; set; }
        public bool ShowRatings { get; set; }
        public bool ShowPrice { get; set; }
        public bool ShowBasePrice { get; set; }
        public bool ShowShippingSurcharge { get; set; }
        public bool ShowButtons { get; set; }
        public bool ShowDiscountBadge { get; set; }
        public bool ShowNewBadge { get; set; }
        public bool BuyEnabled { get; set; }
        public bool WishlistEnabled { get; set; }
        public bool CompareEnabled { get; set; }
        public bool ForceRedirectionAfterAddingToCart { get; set; }
        public DeliveryTimesPresentation DeliveryTimesPresentation { get; set; }

        public List<SummaryItem> Items { get; set; } = new();

        #region IListActions

        public ProductSummaryViewMode ViewMode { get; set; }
        public GridColumnSpan GridColumnSpan { get; set; }
        public bool BoxedStyleItems { get; set; }

        public bool AllowViewModeChanging { get; set; }
        public bool AllowFiltering { get; set; }
        public bool AllowSorting { get; set; }
        public int? CurrentSortOrder { get; set; }
        public string CurrentSortOrderName { get; set; }
        public string RelevanceSortOrderName { get; set; }
        public Dictionary<int, string> AvailableSortOptions { get; set; } = new();

        public IPageable PagedList { get; }
        public IEnumerable<int> AvailablePageSizes { get; set; } = Array.Empty<int>();

        public void Dispose()
        {
            if (Items != null)
            {
                Items.Clear();
            }
        }

        #endregion

        public class SummaryItem : EntityModelBase
        {
            private readonly WeakReference<ProductSummaryModel> _parent;

            public SummaryItem(ProductSummaryModel parent)
            {
                //Parent = parent;
                _parent = new WeakReference<ProductSummaryModel>(parent);

                Weight = string.Empty;
                Price = new PriceModel();
                Image = new ImageModel();
                Attributes = new List<Attribute>();
                SpecificationAttributes = new List<ProductSpecificationModel>();
                Badges = new List<Badge>();
            }

            public ProductSummaryModel Parent
            {
                get
                {
                    _parent.TryGetTarget(out var parent);
                    return parent;
                }
            }

            public LocalizedValue<string> Name { get; set; }
            public LocalizedValue<string> ShortDescription { get; set; }
            public LocalizedValue<string> FullDescription { get; set; }
            public string SeName { get; set; }
            public string DetailUrl { get; set; }
            public string Sku { get; set; }
            public string Weight { get; set; }
            public string Dimensions { get; set; }
            public string DimensionMeasureUnit { get; set; }
            public string LegalInfo { get; set; }
            public Money? TransportSurcharge { get; set; }
            public int RatingSum { get; set; }
            public int TotalReviews { get; set; }
            public string BasePriceInfo { get; set; }
            public PriceDisplayStyle PriceDisplayStyle { get; set; }
            public bool DisplayTextForZeroPrices { get; set; }

            public bool IsShippingEnabled { get; set; }
            public bool HideDeliveryTime { get; set; }
            public string DeliveryTimeDate { get; set; }
            public LocalizedValue<string> DeliveryTimeName { get; set; }
            public string DeliveryTimeHexValue { get; set; }
            public bool DisplayDeliveryTimeAccordingToStock { get; set; }
            public string StockAvailablity { get; set; }

            public int MinPriceProductId { get; set; } // Internal

            public BrandOverviewModel Brand { get; set; }
            public PriceModel Price { get; set; }
            public ImageModel Image { get; set; }
            public IList<Attribute> Attributes { get; set; }
            // TODO: (mc) Let the user specify in attribute manager which spec attributes are
            // important. According to it's importance, show attribute value in grid or list mode.
            // E.g. perfect for "Energy label" > "EEK A++", or special material (e.g. "Leather") etc.
            public IList<ProductSpecificationModel> SpecificationAttributes { get; set; }
            public IList<ColorAttributeValue> ColorAttributes { get; set; }
            public IList<Badge> Badges { get; set; }
        }

        public class PriceModel
        {
            public Money? RegularPrice { get; set; }
            public Money Price { get; set; }

            public bool HasDiscount { get; set; }
            public float SavingPercent { get; set; }
            public Money? SavingAmount { get; set; }

            public bool DisableBuyButton { get; set; }
            public bool DisableWishlistButton { get; set; }

            public bool AvailableForPreOrder { get; set; }
            public bool CallForPrice { get; set; }
        }

        public class ColorAttribute
        {
            public ColorAttribute(int id, string name, IEnumerable<ColorAttributeValue> values)
            {
                Id = id;
                Name = name;
                Values = new HashSet<ColorAttributeValue>(values);
            }

            public int Id { get; private set; }
            public string Name { get; private set; }
            public ICollection<ColorAttributeValue> Values { get; private set; }
        }

        public class ColorAttributeValue
        {
            public int AttributeId { get; set; }
            public LocalizedValue<string> AttributeName { get; set; }
            public int ProductAttributeId { get; set; }

            public int Id { get; set; }
            public string Color { get; set; }
            public string Alias { get; set; }
            public LocalizedValue<string> FriendlyName { get; set; }
            public string ProductUrl { get; set; }

            public override int GetHashCode()
                => Color.GetHashCode();

            public override bool Equals(object obj)
            {
                var equals = base.Equals(obj);
                if (!equals)
                {
                    var o2 = obj as ColorAttributeValue;
                    if (o2 != null)
                    {
                        equals = Color.EqualsNoCase(o2.Color);
                    }
                }

                return equals;
            }
        }

        public class Attribute
        {
            public int Id { get; set; }
            public LocalizedValue<string> Name { get; set; }
            public string Alias { get; set; }
        }

        public class Badge
        {
            public string Label { get; set; }
            public BadgeStyle Style { get; set; }
            public int DisplayOrder { get; set; }
        }
    }

    public enum ProductSummaryViewMode
    {
        Mini,
        Grid,
        List,
        Compare
    }
}