using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.Search;

namespace Smartstore.Web.Models.Catalog
{
    public partial class BrandModel : EntityModelBase, ISearchResultModel
    {
        public CatalogSearchResult SearchResult { get; set; }

        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public LocalizedValue<string> BottomDescription { get; set; }
        public LocalizedValue<string> MetaKeywords { get; set; }
        public LocalizedValue<string> MetaDescription { get; set; }
        public LocalizedValue<string> MetaTitle { get; set; }

        public string SeName { get; set; }
        public string CanonicalUrl { get; set; }
        public ImageModel Image { get; set; } = new();

        public ProductSummaryModel FeaturedProducts { get; set; }
        public ProductSummaryModel Products { get; set; }
        public MetaPropertiesModel MetaProperties { get; set; } = new();
    }
}
