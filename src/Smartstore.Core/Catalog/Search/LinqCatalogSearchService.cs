using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private static readonly int[] _priceThresholds = new int[] { 10, 25, 50, 100, 250, 500, 1000 };

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
            Func<Task<IList<Product>>> hitsFactory = null;
            IDictionary<string, FacetGroup> facets = null;

            if (searchQuery.Take > 0)
            {
                var query = GetProductQuery(searchQuery, null);

                totalHits = await query.CountAsync();

                // Fix paging boundaries.
                if (searchQuery.Skip > 0 && searchQuery.Skip >= totalHits)
                {
                    searchQuery.Slice((totalHits / searchQuery.Take) * searchQuery.Take, searchQuery.Take);
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithHits))
                {
                    var skip = searchQuery.PageIndex * searchQuery.Take;

                    query = query
                        .Skip(skip)
                        .Take(searchQuery.Take);

                    hitsEntityIds = query.Select(x => x.Id).ToArray();
                    hitsFactory = async () => await _db.Products.GetManyAsync(hitsEntityIds);
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithFacets) && searchQuery.FacetDescriptors.Any())
                {
                    facets = await GetFacetsAsync(searchQuery, totalHits);
                }
            }

            var result = new CatalogSearchResult(
                null,
                searchQuery,
                totalHits,
                hitsEntityIds,
                hitsFactory,
                null,
                facets);

            var searchedEvent = new CatalogSearchedEvent(searchQuery, result);
            await _services.EventPublisher.PublishAsync(searchedEvent);

            return searchedEvent.Result;
        }

        #region Utilities

        protected virtual IQueryable<Product> GetProductQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery)
        {
            // TODO: (mg) (core) Try to modularize this monolithic function: e.g. ApplyFilters, ApplyAcl, ApplyOrdering etc.
            
            var ordered = false;
            var utcNow = DateTime.UtcNow;
            var categoryId = 0;
            var manufacturerId = 0;
            var query = baseQuery ?? _db.Products;

            query = query.Where(x => !x.IsSystemProduct);
            query = ApplySearchTerm(query, searchQuery, out var isGroupingRequired);

            #region Filters

            var filters = new List<ISearchFilter>();
            FlattenFilters(searchQuery.Filters, filters);

            var productIds = GetIdList(filters, "id");
            if (productIds.Any())
            {
                query = query.Where(x => productIds.Contains(x.Id));
            }

            var categoryIds = GetIdList(filters, "categoryid");
            if (categoryIds.Any())
            {
                isGroupingRequired = true;
                categoryId = categoryIds.First();
                if (categoryIds.Count == 1 && categoryId == 0)
                {
                    // Has no category.
                    query = query.Where(x => x.ProductCategories.Count == 0);
                }
                else
                {
                    query = ApplyCategoriesFilter(query, categoryIds, null);
                }
            }

            var featuredCategoryIds = GetIdList(filters, "featuredcategoryid");
            var notFeaturedCategoryIds = GetIdList(filters, "notfeaturedcategoryid");
            if (featuredCategoryIds.Any())
            {
                isGroupingRequired = true;
                categoryId = categoryId == 0 ? featuredCategoryIds.First() : categoryId;
                query = ApplyCategoriesFilter(query, featuredCategoryIds, true);
            }
            if (notFeaturedCategoryIds.Any())
            {
                isGroupingRequired = true;
                categoryId = categoryId == 0 ? notFeaturedCategoryIds.First() : categoryId;
                query = ApplyCategoriesFilter(query, notFeaturedCategoryIds, false);
            }

            var manufacturerIds = GetIdList(filters, "manufacturerid");
            if (manufacturerIds.Any())
            {
                isGroupingRequired = true;
                manufacturerId = manufacturerIds.First();
                if (manufacturerIds.Count == 1 && manufacturerId == 0)
                {
                    // Has no manufacturer.
                    query = query.Where(x => x.ProductManufacturers.Count == 0);
                }
                else
                {
                    query = ApplyManufacturersFilter(query, manufacturerIds, null);
                }
            }

            var featuredManuIds = GetIdList(filters, "featuredmanufacturerid");
            var notFeaturedManuIds = GetIdList(filters, "notfeaturedmanufacturerid");
            if (featuredManuIds.Any())
            {
                isGroupingRequired = true;
                manufacturerId = manufacturerId == 0 ? featuredManuIds.First() : manufacturerId;
                query = ApplyManufacturersFilter(query, featuredManuIds, true);
            }
            if (notFeaturedManuIds.Any())
            {
                isGroupingRequired = true;
                manufacturerId = manufacturerId == 0 ? notFeaturedManuIds.First() : manufacturerId;
                query = ApplyManufacturersFilter(query, notFeaturedManuIds, false);
            }

            var tagIds = GetIdList(filters, "tagid");
            if (tagIds.Any())
            {
                isGroupingRequired = true;
                query =
                    from p in query
                    from pt in p.ProductTags.Where(pt => tagIds.Contains(pt.Id))
                    select p;
            }

            if (!_db.QuerySettings.IgnoreAcl)
            {
                var roleIds = GetIdList(filters, "roleid");
                if (roleIds.Any())
                {
                    isGroupingRequired = true;

                    // Do not use ApplyAclFilter to avoid multiple grouping.
                    query =
                        from p in query
                        join acl in _db.AclRecords.AsNoTracking() on new { pid = p.Id, pname = "Product" } equals new { pid = acl.EntityId, pname = acl.EntityName } into pacl
                        from acl in pacl.DefaultIfEmpty()
                        where !p.SubjectToAcl || roleIds.Contains(acl.CustomerRoleId)
                        select p;
                }
            }

            var deliverTimeIds = GetIdList(filters, "deliveryid");
            if (deliverTimeIds.Any())
            {
                query = query.Where(x => x.DeliveryTimeId != null && deliverTimeIds.Contains(x.DeliveryTimeId.Value));
            }

            var parentProductIds = GetIdList(filters, "parentid");
            if (parentProductIds.Any())
            {
                query = query.Where(x => parentProductIds.Contains(x.ParentGroupedProductId));
            }

            var conditions = GetIdList(filters, "condition");
            if (conditions.Any())
            {
                query = query.Where(x => conditions.Contains((int)x.Condition));
            }

            foreach (IAttributeSearchFilter filter in filters)
            {
                var rf = filter as IRangeSearchFilter;

                if (filter.FieldName == "id")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as int?;
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
                }
                else if (filter.FieldName == "categoryid")
                {
                    if (rf != null && 1 == ((filter.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        isGroupingRequired = true;
                        // Has any category.
                        query = query.Where(x => x.ProductCategories.Count > 0);
                    }
                }
                else if (filter.FieldName == "manufacturerid")
                {
                    if (rf != null && 1 == ((filter.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        isGroupingRequired = true;
                        // Has any manufacturer.
                        query = query.Where(x => x.ProductManufacturers.Count > 0);
                    }
                }
                else if (filter.FieldName == "published")
                {
                    query = query.Where(x => x.Published == (bool)filter.Term);
                }
                else if (filter.FieldName == "availablestart")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as DateTime?;
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
                }
                else if (filter.FieldName == "availableend")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as DateTime?;
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
                else if (filter.FieldName == "stockquantity")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as int?;
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
                    else
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.StockQuantity != (int)filter.Term);
                        else
                            query = query.Where(x => x.StockQuantity == (int)filter.Term);
                    }
                }
                else if (filter.FieldName == "rating")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as double?;
                        var upper = rf.UpperTerm as double?;

                        if (lower.HasValue)
                        {
                            if (rf.IncludesLower)
                                query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) >= lower.Value);
                            else
                                query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) > lower.Value);
                        }

                        if (upper.HasValue)
                        {
                            if (rf.IncludesUpper)
                                query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) <= upper.Value);
                            else
                                query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) < upper.Value);
                        }
                    }
                    else
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) != (double)filter.Term);
                        else
                            query = query.Where(x => x.ApprovedTotalReviews != 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) == (double)filter.Term);
                    }
                }
                else if (filter.FieldName == "available")
                {
                    query = query.Where(x =>
                        x.ManageInventoryMethodId == (int)ManageInventoryMethod.DontManageStock ||
                        (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock && (x.StockQuantity > 0 || x.BackorderModeId != (int)BackorderMode.NoBackorders)) ||
                        (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && x.ProductVariantAttributeCombinations.Any(pvac => pvac.StockQuantity > 0 || pvac.AllowOutOfStockOrders))
                    );
                }
                else if (filter.FieldName.StartsWith("price"))
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as double?;
                        var upper = rf.UpperTerm as double?;

                        if (lower.HasValue)
                        {
                            var minPrice = Convert.ToDecimal(lower.Value);

                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < utcNow) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > utcNow))) &&
                                (x.SpecialPrice >= minPrice))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > utcNow) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < utcNow))) &&
                                (x.Price >= minPrice))
                            );
                        }

                        if (upper.HasValue)
                        {
                            var maxPrice = Convert.ToDecimal(upper);

                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < utcNow) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > utcNow))) &&
                                (x.SpecialPrice <= maxPrice))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > utcNow) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < utcNow))) &&
                                (x.Price <= maxPrice))
                            );
                        }
                    }
                    else
                    {
                        var price = Convert.ToDecimal(filter.Term);

                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                        {
                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < utcNow) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > utcNow))) &&
                                (x.SpecialPrice != price))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > utcNow) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < utcNow))) &&
                                (x.Price != price))
                            );
                        }
                        else
                        {
                            query = query.Where(x =>
                                ((x.SpecialPrice.HasValue &&
                                ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < utcNow) &&
                                (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > utcNow))) &&
                                (x.SpecialPrice == price))
                                ||
                                ((!x.SpecialPrice.HasValue ||
                                ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > utcNow) ||
                                (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < utcNow))) &&
                                (x.Price == price))
                            );
                        }
                    }
                }
                else if (filter.FieldName == "createdon")
                {
                    if (rf != null)
                    {
                        var lower = filter.Term as DateTime?;
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
                    else
                    {
                        if (filter.Occurence == SearchFilterOccurence.MustNot)
                            query = query.Where(x => x.CreatedOnUtc != (DateTime)filter.Term);
                        else
                            query = query.Where(x => x.CreatedOnUtc == (DateTime)filter.Term);
                    }
                }
                else if (filter.FieldName == "storeid")
                {
                    if (!_db.QuerySettings.IgnoreMultiStore)
                    {
                        var storeId = (int)filter.Term;
                        if (storeId != 0)
                        {
                            isGroupingRequired = true;

                            // Do not use ApplyStoreFilter to avoid multiple grouping.
                            query =
                                from p in query
                                join sm in _db.StoreMappings.AsNoTracking() on new { pid = p.Id, pname = "Product" } equals new { pid = sm.EntityId, pname = sm.EntityName } into psm
                                from sm in psm.DefaultIfEmpty()
                                where !p.LimitedToStores || sm.StoreId == storeId
                                select p;
                        }
                    }
                }
            }

            #endregion

            // Grouping is very slow if there are many products.
            if (isGroupingRequired)
            {
                query =
                    from p in query
                    group p by p.Id into grp
                    orderby grp.Key
                    select grp.FirstOrDefault();
            }

            #region Sorting

            foreach (var sort in searchQuery.Sorting)
            {
                if (sort.FieldName.IsEmpty())
                {
                    // Sort by relevance.
                    if (categoryId != 0)
                    {
                        query = OrderBy(ref ordered, query, x => x.ProductCategories.Where(pc => pc.CategoryId == categoryId).FirstOrDefault().DisplayOrder);
                    }
                    else if (manufacturerId != 0)
                    {
                        query = OrderBy(ref ordered, query, x => x.ProductManufacturers.Where(pm => pm.ManufacturerId == manufacturerId).FirstOrDefault().DisplayOrder);
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
                if (FindFilter(searchQuery.Filters, "parentid") != null)
                {
                    query = query.OrderBy(x => x.DisplayOrder);
                }
                else
                {
                    query = query.OrderBy(x => x.Id);
                }
            }

            #endregion

            return query;
        }

        protected virtual IQueryable<Product> ApplySearchTerm(IQueryable<Product> query, CatalogSearchQuery searchQuery, out bool isGroupingRequired)
        {
            isGroupingRequired = false;

            var term = searchQuery.Term;
            var fields = searchQuery.Fields;
            var languageId = searchQuery.LanguageId ?? 0;

            if (term.HasValue() && fields != null && fields.Length != 0 && fields.Any(x => x.HasValue()))
            {
                isGroupingRequired = true;

                var lpQuery = _db.LocalizedProperties.AsNoTracking();

                // SearchMode.ExactMatch doesn't make sense here
                if (searchQuery.Mode == SearchMode.StartsWith)
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
                    var customerRolesIds = _services.WorkContext.CurrentCustomer.GetRoleIds();
                    var manufacturersQuery = _db.Manufacturers
                        .AsNoTracking()
                        .ApplyStandardFilter(false, customerRolesIds, storeId);

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
