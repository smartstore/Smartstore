using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Utilities;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductSummaryModel : ModelBase, IListActions, IDisposable
    {
        public readonly static ProductSummaryModel Empty = new(Array.Empty<Product>().ToPagedList(0, int.MaxValue));

        public ProductSummaryModel(IPagedList<Product> products, CatalogSearchResult sourceResult = null)
        {
            Guard.NotNull(products);

            PagedList = products;
            SourceResult = sourceResult;
            Id = CommonHelper.GenerateRandomInteger();
        }

        /// <summary>
        /// A random unique identifier for this model instance to create unique item IDs.
        /// </summary>
        public int Id { get; }

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

        public List<ProductSummaryItemModel> Items { get; set; } = [];

        public ProductSummaryViewMode ViewMode { get; set; }
        public GridColumnSpan GridColumnSpan { get; set; }
        public bool BoxedStyleItems { get; set; }

        public bool AllowViewModeChanging { get; set; }
        public bool AllowFiltering { get; set; }
        public bool AllowSorting { get; set; }
        public int? CurrentSortOrder { get; set; }
        public string CurrentSortOrderName { get; set; }
        public string RelevanceSortOrderName { get; set; }
        public Dictionary<int, string> AvailableSortOptions { get; set; } = [];

        public IPageable PagedList { get; }
        public IEnumerable<int> AvailablePageSizes { get; set; } = [];

        public void Dispose()
        {
            Items?.Clear();
        }
    }
}