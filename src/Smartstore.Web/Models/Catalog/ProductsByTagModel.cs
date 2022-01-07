using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Search;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductsByTagModel : EntityModelBase, ISearchResultModel
    {
        public CatalogSearchResult SearchResult { get; set; }
        public LocalizedValue<string> TagName { get; set; }
        public ProductSummaryModel Products { get; set; }
        public string CanonicalUrl { get; set; }
    }
}
