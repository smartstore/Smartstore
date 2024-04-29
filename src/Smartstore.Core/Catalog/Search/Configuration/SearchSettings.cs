using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Configuration;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class SearchSettings : ISettings
    {
        private static readonly string[] _instantSearchFields = ["sku", "gtin", "mpn", "manufacturer", "attrname", "variantname"];

        /// <summary>
        /// Gets or sets the search mode
        /// </summary>
        public SearchMode SearchMode { get; set; } = SearchMode.Contains;

        /// <summary>
        /// Gets or sets name of fields to be searched. The name field is always searched.
        /// </summary>
        public List<string> SearchFields { get; set; } = ["sku", "shortdescription", "tagname", "manufacturer", "category"];

        /// <summary>
        /// Gets or sets a value indicating whether instant-search is enabled
        /// </summary>
        public bool InstantSearchEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product images in instant-search result
        /// </summary>
        public bool ShowProductImagesInInstantSearch { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of products to return when using "instant-search" feature
        /// </summary>
        public int InstantSearchNumberOfProducts { get; set; } = 10;

        /// <summary>
        /// Gets or sets a minimum instant-search term length
        /// </summary>
        public int InstantSearchTermMinLength { get; set; } = 2;

        /// <summary>
        /// Gets or sets the minimum hit count for a filter value. Values with a lower hit count are not displayed.
        /// </summary>
        public int FilterMinHitCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of filter values to be displayed.
        /// </summary>
        public int FilterMaxChoicesCount { get; set; } = 20;

        /// <summary>
        /// Gets or sets the default sort order in search results
        /// </summary>
        public ProductSortingEnum DefaultSortOrder { get; set; } = ProductSortingEnum.Relevance;

        /// <summary>
        /// Hidden setting indicating whether to use catalog search instead of Linq search in backend.
        /// </summary>
        public bool UseCatalogSearchInBackend { get; set; }

        /// <summary>
        /// Specifies whether the product page should be opened directly if the search term matches a SKU, MPN or GTIN.
        /// </summary>
        public bool SearchProductByIdentificationNumber { get; set; }

        // TBD: what about area specific searchin setting (product, blog, etc.)

        #region Common facet settings

        /// <summary>
        /// Gets or sets the a value indicating whether to include or exclude not available products by default.
        /// </summary>
        public bool IncludeNotAvailable { get; set; }

        public bool BrandDisabled { get; set; }
        public bool PriceDisabled { get; set; }
        public bool RatingDisabled { get; set; }
        public bool DeliveryTimeDisabled { get; set; }
        public bool AvailabilityDisabled { get; set; }
        public bool NewArrivalsDisabled { get; set; }

        public int BrandDisplayOrder { get; set; } = 1;
        public int PriceDisplayOrder { get; set; } = 2;
        public int RatingDisplayOrder { get; set; } = 3;
        public int DeliveryTimeDisplayOrder { get; set; } = 4;
        public int AvailabilityDisplayOrder { get; set; } = 5;
        public int NewArrivalsDisplayOrder { get; set; } = 6;

        #endregion

        /// <summary>
        /// Gets a list of search fields based on <see cref="SearchFields"/>.
        /// </summary>
        /// <param name="forInstantSearch">
        /// A value indicating whether to return fields for instant search.
        /// Returns the fields for search page if <c>false</c>.
        /// </param>
        public List<string> GetSearchFields(bool forInstantSearch)
        {
            List<string> fields;

            if (forInstantSearch)
            {
                fields = ["name", "shortdescription", "tagname"];

                foreach (var fieldName in _instantSearchFields)
                {
                    if (SearchFields?.Contains(fieldName) ?? false)
                    {
                        fields.Add(fieldName);
                    }
                }
            }
            else
            {
                fields = ["name"];

                if (SearchFields != null)
                {
                    fields.AddRange(SearchFields);
                }
            }

            return fields;
        }
    }
}
