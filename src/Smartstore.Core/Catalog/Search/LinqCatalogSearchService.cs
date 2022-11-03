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
        private static readonly int[] _priceThresholds = new[] { 10, 25, 50, 100, 250, 500, 1000 };

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICategoryService _categoryService;

        public LinqCatalogSearchService(
            SmartDbContext db,
            ICommonServices services,
            ICategoryService categoryService)
        {
            _db = db;
            _services = services;
            _categoryService = categoryService;
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
                //totalHits = await query.Select(x => x.Id).Distinct().CountAsync();

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
            var ctx = new QueryBuilderContext
            {
                SearchQuery = searchQuery
            };

            FlattenFilters(searchQuery.Filters, ctx.Filters);

            var query = baseQuery ?? _db.Products;
            query = query.Where(x => !x.IsSystemProduct);
            query = ApplySearchTerm(ctx, query);

            var productIds = GetIdList(ctx.Filters, "id");
            if (productIds.Any())
            {
                query = query.Where(x => productIds.Contains(x.Id));
            }

            var categoryIds = GetIdList(ctx.Filters, "categoryid");
            if (categoryIds.Any())
            {
                ctx.CategoryId ??= categoryIds.First();
                if (categoryIds.Count == 1 && ctx.CategoryId == 0)
                {
                    // Has no category.
                    query = query.Where(x => x.ProductCategories.Count == 0);
                }
                else
                {
                    ctx.IsGroupingRequired = true;
                    query = ApplyCategoriesFilter(query, categoryIds, null);
                }
            }

            var featuredCategoryIds = GetIdList(ctx.Filters, "featuredcategoryid");
            var notFeaturedCategoryIds = GetIdList(ctx.Filters, "notfeaturedcategoryid");
            if (featuredCategoryIds.Any())
            {
                ctx.IsGroupingRequired = true;
                ctx.CategoryId ??= featuredCategoryIds.First();
                query = ApplyCategoriesFilter(query, featuredCategoryIds, true);
            }
            if (notFeaturedCategoryIds.Any())
            {
                ctx.IsGroupingRequired = true;
                ctx.CategoryId ??= notFeaturedCategoryIds.First();
                query = ApplyCategoriesFilter(query, notFeaturedCategoryIds, false);
            }

            var manufacturerIds = GetIdList(ctx.Filters, "manufacturerid");
            if (manufacturerIds.Any())
            {
                ctx.ManufacturerId ??= manufacturerIds.First();
                if (manufacturerIds.Count == 1 && ctx.ManufacturerId == 0)
                {
                    // Has no manufacturer.
                    query = query.Where(x => x.ProductManufacturers.Count == 0);
                }
                else
                {
                    ctx.IsGroupingRequired = true;
                    query = ApplyManufacturersFilter(query, manufacturerIds, null);
                }
            }

            var featuredManuIds = GetIdList(ctx.Filters, "featuredmanufacturerid");
            var notFeaturedManuIds = GetIdList(ctx.Filters, "notfeaturedmanufacturerid");
            if (featuredManuIds.Any())
            {
                ctx.IsGroupingRequired = true;
                ctx.ManufacturerId ??= featuredManuIds.First();
                query = ApplyManufacturersFilter(query, featuredManuIds, true);
            }
            if (notFeaturedManuIds.Any())
            {
                ctx.IsGroupingRequired = true;
                ctx.ManufacturerId ??= notFeaturedManuIds.First();
                query = ApplyManufacturersFilter(query, notFeaturedManuIds, false);
            }

            var tagIds = GetIdList(ctx.Filters, "tagid");
            if (tagIds.Any())
            {
                ctx.IsGroupingRequired = true;
                query =
                    from p in query
                    from pt in p.ProductTags.Where(pt => tagIds.Contains(pt.Id))
                    select p;
            }

            var deliverTimeIds = GetIdList(ctx.Filters, "deliveryid");
            if (deliverTimeIds.Any())
            {
                query = query.Where(x => x.DeliveryTimeId != null && deliverTimeIds.Contains(x.DeliveryTimeId.Value));
            }

            var parentProductIds = GetIdList(ctx.Filters, "parentid");
            if (parentProductIds.Any())
            {
                query = query.Where(x => parentProductIds.Contains(x.ParentGroupedProductId));
            }

            var conditions = GetIdList(ctx.Filters, "condition");
            if (conditions.Any())
            {
                query = query.Where(x => conditions.Contains((int)x.Condition));
            }

            foreach (IAttributeSearchFilter filter in ctx.Filters)
            {
                if (filter is IRangeSearchFilter rf)
                {
                    query = ApplyRangeFilter(ctx, query, rf);
                }
                else
                {
                    // Filters that can have both range and comparison values.
                    if (filter.FieldName == "stockquantity")
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.StockQuantity != (int)filter.Term);
                        else
                            query = query.Where(x => x.StockQuantity == (int)filter.Term);
                    }
                    else if (filter.FieldName == "rating")
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) != (double)filter.Term);
                        else
                            query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) == (double)filter.Term);
                    }
                    else if (filter.FieldName == "createdon")
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.CreatedOnUtc != (DateTime)filter.Term);
                        else
                            query = query.Where(x => x.CreatedOnUtc == (DateTime)filter.Term);
                    }
                    else if (filter.FieldName.StartsWith("price"))
                    {
                        var price = Convert.ToDecimal(filter.Term);

                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                        {
                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < ctx.Now) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > ctx.Now))) &&
                                (x.SpecialPrice != price))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > ctx.Now) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < ctx.Now))) &&
                                (x.Price != price))
                            );
                        }
                        else
                        {
                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < ctx.Now) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > ctx.Now))) &&
                                (x.SpecialPrice == price))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > ctx.Now) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < ctx.Now))) &&
                                (x.Price == price))
                            );
                        }
                    }
                }

                if (filter.FieldName == "published")
                {
                    query = query.Where(x => x.Published == (bool)filter.Term);
                }
                else if (filter.FieldName == "visibility")
                {
                    var visibility = (ProductVisibility)filter.Term;
                    query = visibility switch
                    {
                        ProductVisibility.SearchResults => query.Where(x => x.Visibility <= visibility),
                        _ => query.Where(x => x.Visibility == visibility),
                    };
                }
                else if (filter.FieldName == "showonhomepage")
                {
                    query = query.Where(p => p.ShowOnHomePage == (bool)filter.Term);
                }
                else if (filter.FieldName == "download")
                {
                    query = query.Where(p => p.IsDownload == (bool)filter.Term);
                }
                else if (filter.FieldName == "recurring")
                {
                    query = query.Where(p => p.IsRecurring == (bool)filter.Term);
                }
                else if (filter.FieldName == "shipenabled")
                {
                    query = query.Where(p => p.IsShippingEnabled == (bool)filter.Term);
                }
                else if (filter.FieldName == "shipfree")
                {
                    query = query.Where(p => p.IsFreeShipping == (bool)filter.Term);
                }
                else if (filter.FieldName == "taxexempt")
                {
                    query = query.Where(p => p.IsTaxExempt == (bool)filter.Term);
                }
                else if (filter.FieldName == "esd")
                {
                    query = query.Where(p => p.IsEsd == (bool)filter.Term);
                }
                else if (filter.FieldName == "discount")
                {
                    query = query.Where(p => p.HasDiscountsApplied == (bool)filter.Term);
                }
                else if (filter.FieldName == "typeid")
                {
                    query = query.Where(x => x.ProductTypeId == (int)filter.Term);
                }
                else if (filter.FieldName == "available")
                {
                    query = query.Where(x =>
                        x.ManageInventoryMethodId == (int)ManageInventoryMethod.DontManageStock ||
                        (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock && (x.StockQuantity > 0 || x.BackorderModeId != (int)BackorderMode.NoBackorders)) ||
                        (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && x.ProductVariantAttributeCombinations.Any(pvac => pvac.StockQuantity > 0 || pvac.AllowOutOfStockOrders))
                    );
                }
            }

            query = ApplyAclFilter(ctx, query);
            query = ApplyStoreFilter(ctx, query);

            // Not supported by EF Core 5.0.
            //if (ctx.IsGroupingRequired)
            //{
            //    query =
            //        from p in query
            //        group p by p.Id into grp
            //        orderby grp.Key
            //        select grp.FirstOrDefault();
            //}

            // INFO: Distinct does not preserve ordering.
            if (ctx.IsGroupingRequired)
            {
                // Distinct is very slow if there are many products.
                query = query.Distinct();
            }

            query = ApplyOrdering(ctx, query);

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

        protected virtual IQueryable<Product> ApplySearchTerm(QueryBuilderContext ctx, IQueryable<Product> query)
        {
            var term = ctx.SearchQuery.Term;
            var fields = ctx.SearchQuery.Fields;
            var languageId = ctx.SearchQuery.LanguageId ?? 0;

            if (term.HasValue() && fields != null && fields.Length != 0 && fields.Any(x => x.HasValue()))
            {
                ctx.IsGroupingRequired = true;

                var lpQuery = _db.LocalizedProperties.AsNoTracking();

                // SearchMode.ExactMatch doesn't make sense here
                if (ctx.SearchQuery.Mode == SearchMode.StartsWith)
                {
                    query =
                        from p in query
                        join lp in lpQuery on p.Id equals lp.EntityId into plp
                        from lp in plp.DefaultIfEmpty()
                        where
                        (fields.Contains("name") && p.Name.StartsWith(term)) ||
                        (fields.Contains("sku") && p.Sku.StartsWith(term)) ||
                        (fields.Contains("shortdescription") && p.ShortDescription.StartsWith(term)) ||
                        (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "Name" && lp.LocaleValue.StartsWith(term)) ||
                        (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "ShortDescription" && lp.LocaleValue.StartsWith(term))
                        select p;
                }
                else
                {
                    query =
                        from p in query
                        join lp in lpQuery on p.Id equals lp.EntityId into plp
                        from lp in plp.DefaultIfEmpty()
                        where
                        (fields.Contains("name") && p.Name.Contains(term)) ||
                        (fields.Contains("sku") && p.Sku.Contains(term)) ||
                        (fields.Contains("shortdescription") && p.ShortDescription.Contains(term)) ||
                        (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "Name" && lp.LocaleValue.Contains(term)) ||
                        (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "ShortDescription" && lp.LocaleValue.Contains(term))
                        select p;
                }
            }

            return query;
        }

        private static IQueryable<Product> ApplyRangeFilter(QueryBuilderContext ctx, IQueryable<Product> query, IRangeSearchFilter rf)
        {
            if (rf.FieldName == "categoryid")
            {
                // Has any category.
                if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                {
                    query = query.Where(x => x.ProductCategories.Count > 0);
                }
            }
            else if (rf.FieldName == "manufacturerid")
            {
                // Has any manufacturer.
                if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                {
                    query = query.Where(x => x.ProductManufacturers.Count > 0);
                }
            }
            else if (rf.FieldName == "id")
            {
                var lower = rf.Term as int?;
                var upper = rf.UpperTerm as int?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.Id >= lower.Value);
                    else
                        query = query.Where(x => x.Id > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesUpper)
                        query = query.Where(x => x.Id <= upper.Value);
                    else
                        query = query.Where(x => x.Id < upper.Value);
                }
            }
            else if (rf.FieldName == "availablestart")
            {
                var lower = rf.Term as DateTime?;
                var upper = rf.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc >= lower.Value);
                    else
                        query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc <= upper.Value);
                    else
                        query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc < upper.Value);
                }
            }
            else if (rf.FieldName == "availableend")
            {
                var lower = rf.Term as DateTime?;
                var upper = rf.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc >= lower.Value);
                    else
                        query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc <= upper.Value);
                    else
                        query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc < upper.Value);
                }
            }
            else if (rf.FieldName == "stockquantity")
            {
                var lower = rf.Term as int?;
                var upper = rf.UpperTerm as int?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.StockQuantity >= lower.Value);
                    else
                        query = query.Where(x => x.StockQuantity > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesUpper)
                        query = query.Where(x => x.StockQuantity <= upper.Value);
                    else
                        query = query.Where(x => x.StockQuantity < upper.Value);
                }
            }
            else if (rf.FieldName == "rating")
            {
                var lower = rf.Term as double?;
                var upper = rf.UpperTerm as double?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) >= lower.Value);
                    else
                        query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesUpper)
                        query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) <= upper.Value);
                    else
                        query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) < upper.Value);
                }
            }
            else if (rf.FieldName == "createdon")
            {
                var lower = rf.Term as DateTime?;
                var upper = rf.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.CreatedOnUtc >= lower.Value);
                    else
                        query = query.Where(x => x.CreatedOnUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.CreatedOnUtc <= upper.Value);
                    else
                        query = query.Where(x => x.CreatedOnUtc < upper.Value);
                }
            }
            else if (rf.FieldName.StartsWith("price"))
            {
                var lower = rf.Term as double?;
                var upper = rf.UpperTerm as double?;

                if (lower.HasValue)
                {
                    var minPrice = Convert.ToDecimal(lower.Value);

                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < ctx.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > ctx.Now))) &&
                        (x.SpecialPrice >= minPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > ctx.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < ctx.Now))) &&
                        (x.Price >= minPrice))
                    );
                }

                if (upper.HasValue)
                {
                    var maxPrice = Convert.ToDecimal(upper);

                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < ctx.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > ctx.Now))) &&
                        (x.SpecialPrice <= maxPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > ctx.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < ctx.Now))) &&
                        (x.Price <= maxPrice))
                    );
                }
            }

            return query;
        }

        private static IQueryable<Product> ApplyCategoriesFilter(IQueryable<Product> query, List<int> ids, bool? featuredOnly)
        {
            return
                from p in query
                from pc in p.ProductCategories.Where(pc => ids.Contains(pc.CategoryId))
                where !featuredOnly.HasValue || featuredOnly.Value == pc.IsFeaturedProduct
                select p;
        }

        private static IQueryable<Product> ApplyManufacturersFilter(IQueryable<Product> query, List<int> ids, bool? featuredOnly)
        {
            return
                from p in query
                from pm in p.ProductManufacturers.Where(pm => ids.Contains(pm.ManufacturerId))
                where !featuredOnly.HasValue || featuredOnly.Value == pm.IsFeaturedProduct
                select p;
        }

        private IQueryable<Product> ApplyStoreFilter(QueryBuilderContext ctx, IQueryable<Product> query)
        {
            if (!_db.QuerySettings.IgnoreMultiStore)
            {
                var storeIds = GetIdList(ctx.Filters, "storeid");
                if (storeIds.Any())
                {
                    var entityName = nameof(Product);
                    var subQuery = _db.StoreMappings
                        .Where(x => x.EntityName == entityName && storeIds.Contains(x.StoreId))
                        .Select(x => x.EntityId);

                    query = query.Where(x => !x.LimitedToStores || subQuery.Contains(x.Id));
                }
            }

            return query;
        }

        private IQueryable<Product> ApplyAclFilter(QueryBuilderContext ctx, IQueryable<Product> query)
        {
            if (!_db.QuerySettings.IgnoreAcl)
            {
                var roleIds = GetIdList(ctx.Filters, "roleid");
                if (roleIds.Any())
                {
                    var entityName = nameof(Product);
                    var subQuery = _db.AclRecords
                        .Where(x => x.EntityName == entityName && roleIds.Contains(x.CustomerRoleId))
                        .Select(x => x.EntityId);

                    query = query.Where(x => !x.SubjectToAcl || subQuery.Contains(x.Id));
                }
            }

            return query;
        }

        private IQueryable<Product> ApplyOrdering(QueryBuilderContext ctx, IQueryable<Product> query)
        {
            var ordered = false;

            foreach (var sort in ctx.SearchQuery.Sorting)
            {
                if (sort.FieldName.IsEmpty())
                {
                    // Sort by relevance.
                    if (ctx.CategoryId > 0)
                    {
                        query = OrderBy(ref ordered, query, x => x.ProductCategories.Where(pc => pc.CategoryId == ctx.CategoryId.Value).FirstOrDefault().DisplayOrder);
                    }
                    else if (ctx.ManufacturerId > 0)
                    {
                        query = OrderBy(ref ordered, query, x => x.ProductManufacturers.Where(pm => pm.ManufacturerId == ctx.ManufacturerId.Value).FirstOrDefault().DisplayOrder);
                    }
                }
                else if (sort.FieldName == "createdon")
                {
                    query = OrderBy(ref ordered, query, x => x.CreatedOnUtc, sort.Descending);
                }
                else if (sort.FieldName == "name")
                {
                    query = OrderBy(ref ordered, query, x => x.Name, sort.Descending);
                }
                else if (sort.FieldName == "price")
                {
                    query = OrderBy(ref ordered, query, x => x.Price, sort.Descending);
                }
            }

            if (!ordered)
            {
                if (FindFilter(ctx.SearchQuery.Filters, "parentid") != null)
                {
                    query = query.OrderBy(x => x.DisplayOrder);
                }
                else
                {
                    query = query.OrderBy(x => x.Id);
                }
            }

            return query;
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

        protected class QueryBuilderContext
        {
            public CatalogSearchQuery SearchQuery { get; init; }
            public List<ISearchFilter> Filters { get; init; } = new();
            public DateTime Now { get; init; } = DateTime.UtcNow;
            public bool IsGroupingRequired { get; set; }
            public int? CategoryId { get; set; }
            public int? ManufacturerId { get; set; }
        }
    }
}
