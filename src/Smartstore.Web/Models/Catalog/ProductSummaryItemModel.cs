using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public class ProductSummaryItemContext
    {
        public ProductSummaryModel Model { get; set; }
        public ProductSummaryMappingSettings MappingSettings { get; set; }
        public ProductBatchContext BatchContext { get; set; }
        public PriceCalculationOptions CalculationOptions { get; set; }
        public ProductBatchContext AssociatedProductBatchContext { get; set; }
        public ProductBatchContext BundleItemBatchContext { get; set; }
        public Multimap<int, Product> GroupedProducts { get; set; }
        public Dictionary<int, BrandOverviewModel> CachedBrandModels { get; set; }
        public Dictionary<int, MediaFileInfo> MediaFiles { get; set; } = [];
        public Dictionary<string, LocalizedString> Resources { get; set; }
        public string LegalInfo { get; set; }
        public string TaxExemptLegalInfo { get; set; }
        public Currency PrimaryCurrency { get; set; }

        public bool AllowPrices { get; set; }
        public bool AllowShoppingCart { get; set; }
        public bool AllowWishlist { get; set; }
        public string ShippingChargeTaxFormat { get; set; }

        internal IMapper<Product, ProductSummaryItemModel> CustomMapper { get; set; }
    }

    public class ProductSummaryItemModel : EntityModelBase
    {
        private readonly WeakReference<ProductSummaryModel> _parent;

        public ProductSummaryItemModel(ProductSummaryModel parent)
        {
            _parent = new WeakReference<ProductSummaryModel>(parent);
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
        public string Weight { get; set; } = string.Empty;
        public string Dimensions { get; set; }
        public string DimensionMeasureUnit { get; set; }
        public string LegalInfo { get; set; }
        public Money? TransportSurcharge { get; set; }
        public int RatingSum { get; set; }
        public int TotalReviews { get; set; }
        public int MinPriceProductId { get; set; } // Internal

        public bool IsShippingEnabled { get; set; }
        public DeliveryTimeModel DeliveryTime { get; set; }
        public BrandOverviewModel Brand { get; set; }
        public ProductSummaryPriceModel Price { get; set; } = new();
        public ImageModel Image { get; set; } = new();
        public List<Attribute> Attributes { get; set; } = [];
        // TODO: (mc) Let the user specify in attribute manager which spec attributes are
        // important. According to it's importance, show attribute value in grid or list mode.
        // E.g. perfect for "Energy label" > "EEK A++", or special material (e.g. "Leather") etc.
        public List<ProductSpecificationModel> SpecificationAttributes { get; set; } = [];
        public List<ColorAttributeValue> ColorAttributes { get; set; }
        public List<ProductBadgeModel> Badges { get; set; } = [];

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
                if (!equals && obj is ColorAttributeValue o2)
                {
                    equals = Color.EqualsNoCase(o2.Color);
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
    }
}
