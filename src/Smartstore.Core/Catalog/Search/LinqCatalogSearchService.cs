using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Search
{
    public partial class LinqCatalogSearchService : SearchServiceBase, ICatalogSearchService
    {
        private static readonly int[] _priceThresholds = [10, 25, 50, 100, 250, 500, 1000];

        private readonly SmartDbContext _db;
        private readonly LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>[] _queryVisitors;
        private readonly ICommonServices _services;
        private readonly ICategoryService _categoryService;
        private readonly SearchSettings _searchSettings;

        public LinqCatalogSearchService(
            SmartDbContext db,
            IEnumerable<LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>> queryVisitors,
            ICommonServices services,
            ICategoryService categoryService,
            SearchSettings searchSettings)
        {
            _db = db;
            _queryVisitors = [.. queryVisitors.OrderBy(x => x.Order)];
            _services = services;
            _categoryService = categoryService;
            _searchSettings = searchSettings;
        }

        public IQueryable<Product> PrepareQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery = null)
        {
            return GetProductQuery(searchQuery, baseQuery);
        }

        public async Task<CatalogSearchResult> SearchAsync(CatalogSearchQuery searchQuery, bool direct = false)
        {
            await _services.EventPublisher.PublishAsync(new CatalogSearchingEvent(searchQuery, true));

            var totalHits = 0;
            int[] hitsEntityIds = null;
            IDictionary<string, FacetGroup> facets = null;

            if (searchQuery.Take > 0)
            {
                var query = GetProductQuery(searchQuery, null);

                totalHits = await query.CountAsync();

                // Fix paging boundaries.
                if (searchQuery.Skip > 0 && searchQuery.Skip > totalHits)
                {
                    searchQuery.Slice(totalHits, searchQuery.Take);
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithHits))
                {
                    var skip = searchQuery.PageIndex * searchQuery.Take;

                    query = query
                        .Skip(skip)
                        .Take(searchQuery.Take);

                    hitsEntityIds = query.Select(x => x.Id).ToArray();
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithFacets) && searchQuery.FacetDescriptors.Any())
                {
                    facets = await GetFacetsAsync(searchQuery, totalHits);
                }
            }
            
            var result = new CatalogSearchResult(
                null,
                searchQuery,
                _db.Products,
                totalHits,
                hitsEntityIds,
                null,
                facets);

            var searchedEvent = new CatalogSearchedEvent(searchQuery, result);
            await _services.EventPublisher.PublishAsync(searchedEvent);

            return searchedEvent.Result;
        }

        #region Utilities

        protected virtual IQueryable<Product> GetProductQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery)
        {
            // Create context
            var context = new CatalogSearchQueryContext(searchQuery, _services, _searchSettings);

            // Prepare base db query
            var query = baseQuery ?? _db.Products;
            query = query.Where(x => !x.IsSystemProduct);

            // Run all visitors
            foreach (var visitor in _queryVisitors)
            {
                query = visitor.Visit(context, query);
            }

            return query;
        }

        protected virtual async Task<IDictionary<string, FacetGroup>> GetFacetsAsync(CatalogSearchQuery searchQuery, int totalHits)
        {
            var result = new Dictionary<string, FacetGroup>();
            var storeId = searchQuery.StoreId ?? _services.StoreContext.CurrentStore.Id;
            var languageId = searchQuery.LanguageId ?? _services.WorkContext.WorkingLanguage.Id;

            foreach (var key in searchQuery.FacetDescriptors.Keys)
            {
                var descriptor = searchQuery.FacetDescriptors[key];
                var facets = new List<Facet>();
                var kind = FacetGroup.GetKindByKey(CatalogSearchService.Scope, key);

                switch (kind)
                {
                    case FacetGroupKind.Category:
                    case FacetGroupKind.Brand:
                    case FacetGroupKind.DeliveryTime:
                    case FacetGroupKind.Rating:
                    case FacetGroupKind.Price:
                        if (totalHits == 0 && !descriptor.Values.Any(x => x.IsSelected))
                        {
                            continue;
                        }
                        break;
                }

                if (kind == FacetGroupKind.Category)
                {
                    var names = await GetLocalizedNames(nameof(Category), languageId);
                    var categoryTree = await _categoryService.GetCategoryTreeAsync(0, false, storeId);
                    var categories = categoryTree.Flatten(false);

                    if (descriptor.MaxChoicesCount > 0)
                    {
                        categories = categories.Take(descriptor.MaxChoicesCount);
                    }

                    foreach (var category in categories)
                    {
                        names.TryGetValue(category.Id, out var label);

                        facets.Add(new Facet(new FacetValue(category.Id, IndexTypeCode.Int32)
                        {
                            IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(category.Id)),
                            Label = label.HasValue() ? label : category.Name,
                            DisplayOrder = category.DisplayOrder
                        }));
                    }
                }
                else if (kind == FacetGroupKind.Brand)
                {
                    var names = await GetLocalizedNames(nameof(Manufacturer), languageId);
                    var customerRoleIds = _services.WorkContext.CurrentCustomer.GetRoleIds();
                    var manufacturersQuery = _db.Manufacturers
                        .AsNoTracking()
                        .ApplyStandardFilter(false, customerRoleIds, storeId);

                    var manufacturers = descriptor.MaxChoicesCount > 0
                        ? await manufacturersQuery.Take(descriptor.MaxChoicesCount).ToListAsync()
                        : await manufacturersQuery.ToListAsync();

                    foreach (var manu in manufacturers)
                    {
                        names.TryGetValue(manu.Id, out var label);

                        facets.Add(new Facet(new FacetValue(manu.Id, IndexTypeCode.Int32)
                        {
                            IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(manu.Id)),
                            Label = label.HasValue() ? label : manu.Name,
                            DisplayOrder = manu.DisplayOrder
                        }));
                    }
                }
                else if (kind == FacetGroupKind.DeliveryTime)
                {
                    var names = await GetLocalizedNames(nameof(DeliveryTime), languageId);
                    var deliveryTimesQuery = _db.DeliveryTimes
                        .AsNoTracking()
                        .OrderBy(x => x.DisplayOrder);

                    var deliveryTimes = descriptor.MaxChoicesCount > 0
                        ? await deliveryTimesQuery.Take(descriptor.MaxChoicesCount).ToListAsync()
                        : await deliveryTimesQuery.ToListAsync();

                    foreach (var deliveryTime in deliveryTimes)
                    {
                        names.TryGetValue(deliveryTime.Id, out var label);

                        facets.Add(new Facet(new FacetValue(deliveryTime.Id, IndexTypeCode.Int32)
                        {
                            IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(deliveryTime.Id)),
                            Label = label.HasValue() ? label : deliveryTime.Name,
                            DisplayOrder = deliveryTime.DisplayOrder
                        }));
                    }
                }
                else if (kind == FacetGroupKind.Price)
                {
                    var count = 0;
                    var hasActivePredefinedFacet = false;
                    var minPrice = await _db.Products.Where(x => x.Published && !x.IsSystemProduct).MinAsync(x => (double)x.Price);
                    var maxPrice = await _db.Products.Where(x => x.Published && !x.IsSystemProduct).MaxAsync(x => (double)x.Price);
                    minPrice = FacetUtility.MakePriceEven(minPrice);
                    maxPrice = FacetUtility.MakePriceEven(maxPrice);

                    for (var i = 0; i < _priceThresholds.Length; ++i)
                    {
                        if (descriptor.MaxChoicesCount > 0 && facets.Count >= descriptor.MaxChoicesCount)
                        {
                            break;
                        }

                        var price = _priceThresholds[i];
                        if (price < minPrice)
                        {
                            continue;
                        }

                        if (price >= maxPrice)
                        {
                            i = int.MaxValue - 1;
                        }

                        var selected = descriptor.Values.Any(x => x.IsSelected && x.Value == null && x.UpperValue != null && (double)x.UpperValue == price);
                        if (selected)
                        {
                            hasActivePredefinedFacet = true;
                        }

                        facets.Add(new Facet(new FacetValue(null, price, IndexTypeCode.Double, false, true)
                        {
                            DisplayOrder = ++count,
                            IsSelected = selected
                        }));
                    }

                    // Add facet for custom price range.
                    var priceDescriptorValue = descriptor.Values.FirstOrDefault();

                    var customPriceFacetValue = new FacetValue(
                        priceDescriptorValue != null && !hasActivePredefinedFacet ? priceDescriptorValue.Value : null,
                        priceDescriptorValue != null && !hasActivePredefinedFacet ? priceDescriptorValue.UpperValue : null,
                        IndexTypeCode.Double,
                        true,
                        true);

                    customPriceFacetValue.IsSelected = customPriceFacetValue.Value != null || customPriceFacetValue.UpperValue != null;

                    if (!(totalHits == 0 && !customPriceFacetValue.IsSelected))
                    {
                        facets.Insert(0, new Facet("custom", customPriceFacetValue));
                    }
                }
                else if (kind == FacetGroupKind.Rating)
                {
                    foreach (var rating in FacetUtility.GetRatings())
                    {
                        var newFacet = new Facet(rating);
                        newFacet.Value.IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(rating.Value));
                        facets.Add(newFacet);
                    }
                }
                else if (kind == FacetGroupKind.Availability || kind == FacetGroupKind.NewArrivals)
                {
                    var value = descriptor.Values.FirstOrDefault();
                    if (value != null)
                    {
                        if (kind == FacetGroupKind.NewArrivals && totalHits == 0 && !value.IsSelected)
                        {
                            continue;
                        }

                        var newValue = value.Clone();
                        newValue.Value = true;
                        newValue.TypeCode = IndexTypeCode.Boolean;
                        newValue.IsRange = false;
                        newValue.IsSelected = value.IsSelected;

                        facets.Add(new Facet(newValue));
                    }
                }

                if (facets.Any(x => x.Published))
                {
                    //facets.Each(x => $"{key} {x.Value.ToString()}".Dump());

                    result.Add(key, new FacetGroup(
                        CatalogSearchService.Scope,
                        key,
                        descriptor.Label,
                        descriptor.IsMultiSelect,
                        false,
                        descriptor.DisplayOrder,
                        facets.OrderBy(descriptor)));
                }
            }

            return result;
        }

        private async Task<Dictionary<int, string>> GetLocalizedNames(string entityName, int languageId, string key = "Name")
        {
            var values = await _db.LocalizedProperties
                .Where(x => x.LocaleKeyGroup == entityName && x.LocaleKey == key && x.LanguageId == languageId)
                .Select(x => new { x.EntityId, x.LocaleValue })
                .ToListAsync();

            var result = values.ToDictionarySafe(x => x.EntityId, x => x.LocaleValue);
            return result;
        }

        #endregion
    }
}