using System.Text;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;

namespace Smartstore.Core.Catalog.Search.Modelling
{
    /*
		TOKENS:
		===============================
		q	-	Search term
		i	-	Page index
		s	-	Page size
		o	-	Order by
		p	-   Price range (from~to || from(~) || ~to)
		c	-	Categories
		m	-	Manufacturers
		r	-	Min Rating
		a	-	Availability
		n	-	New Arrivals
		d	-	Delivery Time
		v	-	View Mode
		
		*	-	Variants & attributes
	*/

    public partial class CatalogSearchQueryFactory : SearchQueryFactoryBase, ICatalogSearchQueryFactory
    {
        protected readonly ICommonServices _services;
        protected readonly ICatalogSearchQueryAliasMapper _catalogSearchQueryAliasMapper;
        protected readonly CatalogSettings _catalogSettings;
        protected readonly SearchSettings _searchSettings;

        public CatalogSearchQueryFactory(
            IHttpContextAccessor httpContextAccessor,
            ICommonServices services,
            ICatalogSearchQueryAliasMapper catalogSearchQueryAliasMapper,
            CatalogSettings catalogSettings,
            SearchSettings searchSettings)
            : base(httpContextAccessor)
        {
            _services = services;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
            _catalogSettings = catalogSettings;
            _searchSettings = searchSettings;
        }

        protected override string[] Tokens => new[] { "q", "i", "s", "o", "p", "c", "m", "r", "a", "n", "d", "v" };

        public CatalogSearchQuery Current { get; private set; }

        public async Task<CatalogSearchQuery> CreateFromQueryAsync()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx?.Request == null)
            {
                return null;
            }

            var area = ctx.Request.RouteValues.GetAreaName();
            var controller = ctx.Request.RouteValues.GetControllerName();
            var action = ctx.Request.RouteValues.GetActionName();
            var origin = "{0}{1}/{2}".FormatInvariant(area.HasValue() ? area + "/" : string.Empty, controller, action);
            var isInstantSearch = action.EqualsNoCase("InstantSearch");
            var fields = _searchSettings.GetSearchFields(isInstantSearch);
            var term = GetValueFor<string>("q");

            var query = new CatalogSearchQuery(fields.ToArray(), term, _searchSettings.SearchMode)
                .OriginatesFrom(origin)
                .WithLanguage(_services.WorkContext.WorkingLanguage)
                .WithCurrency(_services.WorkContext.WorkingCurrency)
                .BuildFacetMap(!isInstantSearch);

            // Visibility.
            query.VisibleOnly(!_services.DbContext.QuerySettings.IgnoreAcl ? _services.WorkContext.CurrentCustomer : null);

            if (isInstantSearch || query.IsSearchPage())
            {
                query.WithVisibility(ProductVisibility.SearchResults);
            }
            else
            {
                query.WithVisibility(ProductVisibility.Full);
            }

            // Store.
            if (!_services.DbContext.QuerySettings.IgnoreMultiStore)
            {
                query.HasStoreId(_services.StoreContext.CurrentStore.Id);
            }

            // Availability.
            ConvertAvailability(query, origin);

            // Instant-Search never uses these filter parameters.
            if (!isInstantSearch)
            {
                await ConvertPagingSortingAsync(query, origin);
                ConvertPrice(query, origin);
                ConvertCategory(query, origin);
                ConvertManufacturer(query, origin);
                ConvertRating(query, origin);
                ConvertNewArrivals(query, origin);
                ConvertDeliveryTime(query, origin);
            }

            await OnConvertedAsync(query, origin);

            Current = query;
            return query;
        }

        protected virtual async Task ConvertPagingSortingAsync(CatalogSearchQuery query, string origin)
        {
            var index = Math.Max(1, GetValueFor<int?>("i") ?? 1);
            var size = await GetPageSize(query, origin);

            query.Slice((index - 1) * size, size);

            var orderBy = GetValueFor<ProductSortingEnum?>("o");
            if (orderBy == null || orderBy == ProductSortingEnum.Initial)
            {
                orderBy = query.IsSearchPage() ? _searchSettings.DefaultSortOrder : _catalogSettings.DefaultSortOrder;
            }

            query.CustomData["CurrentSortOrder"] = orderBy.Value;

            query.SortBy(orderBy.Value);
        }

        private async Task<int> GetPageSize(CatalogSearchQuery query, string origin)
        {
            string entityViewMode = null;

            // Determine entity id if possible.
            IPagingOptions entity = null;

            if (origin.EqualsNoCase("Catalog/Category"))
            {
                var entityId = _httpContextAccessor.HttpContext.GetRouteValueAs<int?>("categoryId");
                if (entityId.HasValue)
                {
                    entity = await _services.DbContext.Categories
                        .AsNoTracking()
                        .SelectSummary()
                        .FirstOrDefaultAsync(x => x.Id == entityId.Value);

                    entityViewMode = ((Category)entity)?.DefaultViewMode;
                }
            }
            else if (origin.EqualsNoCase("Catalog/Manufacturer"))
            {
                var entityId = _httpContextAccessor.HttpContext.GetRouteValueAs<int?>("manufacturerId");
                if (entityId.HasValue)
                {
                    entity = await _services.DbContext.Manufacturers
                        .AsNoTracking()
                        .SelectSummary()
                        .FirstOrDefaultAsync(x => x.Id == entityId.Value);
                }
            }

            var entitySize = entity?.PageSize;

            var sessionKey = origin;
            if (entitySize.HasValue)
            {
                sessionKey += "/" + entitySize.Value;
            }

            DetectViewMode(query, sessionKey, entityViewMode);

            var allowChange = entity?.AllowCustomersToSelectPageSize ?? _catalogSettings.AllowCustomersToSelectPageSize;
            if (!allowChange)
            {
                return entitySize ?? _catalogSettings.DefaultProductListPageSize;
            }

            sessionKey = "PageSize:" + sessionKey;

            // Get from form or query.
            var session = _httpContextAccessor.HttpContext?.Session;
            var selectedSize = GetValueFor<int?>("s");

            if (selectedSize.HasValue)
            {
                // Save the selection in session. We'll fetch this session value
                // on subsequent requests for this route.
                if (session != null)
                {
                    session.SetInt32(sessionKey, selectedSize.Value);
                }
                return selectedSize.Value;
            }

            // Return user size from session.
            if (session != null)
            {
                var sessionSize = session.GetInt32(sessionKey);
                if (sessionSize.HasValue)
                {
                    return sessionSize.Value;
                }
            }

            // Return default size for entity (IPagingOptions).
            if (entitySize.HasValue)
            {
                return entitySize.Value;
            }

            // Return default page size.
            return _catalogSettings.DefaultProductListPageSize;
        }

        private void DetectViewMode(CatalogSearchQuery query, string sessionKey, string entityViewMode = null)
        {
            if (!_catalogSettings.AllowProductViewModeChanging)
            {
                query.CustomData["ViewMode"] = entityViewMode.NullEmpty() ?? _catalogSettings.DefaultViewMode;
                return;
            }

            var session = _httpContextAccessor.HttpContext?.Session;
            var selectedViewMode = GetValueFor<string>("v");

            sessionKey = "ViewMode:" + sessionKey;

            if (selectedViewMode != null)
            {
                // Save the view mode selection in session. We'll fetch this session value
                // on subsequent requests for this route.
                if (session != null)
                {
                    session.SetString(sessionKey, selectedViewMode);
                }
                query.CustomData["ViewMode"] = selectedViewMode;
                return;
            }

            // Set view mode from session.
            if (session != null)
            {
                var sessionViewMode = session.GetString(sessionKey);
                if (sessionViewMode != null)
                {
                    query.CustomData["ViewMode"] = sessionViewMode;
                    return;
                }
            }

            // Set default view mode for entity.
            if (entityViewMode != null)
            {
                query.CustomData["ViewMode"] = entityViewMode;
                return;
            }

            // Set default view mode.
            query.CustomData["ViewMode"] = _catalogSettings.DefaultViewMode;
        }

        private void AddFacet(
            CatalogSearchQuery query,
            FacetGroupKind kind,
            bool isMultiSelect,
            FacetSorting sorting,
            Action<FacetDescriptor> addValues)
        {
            string fieldName;
            string labelKey;
            var displayOrder = 0;

            switch (kind)
            {
                case FacetGroupKind.Category:
                    fieldName = _catalogSettings.IncludeFeaturedProductsInNormalLists ? "categoryid" : "notfeaturedcategoryid";
                    labelKey = "Search.Facet.Category";
                    break;
                case FacetGroupKind.Brand:
                    if (_searchSettings.BrandDisabled)
                        return;

                    fieldName = "manufacturerid";
                    labelKey = "Search.Facet.Manufacturer";
                    displayOrder = _searchSettings.BrandDisplayOrder;
                    break;
                case FacetGroupKind.Price:
                    if (_searchSettings.PriceDisabled || !_services.Permissions.Authorize(Permissions.Catalog.DisplayPrice))
                        return;

                    fieldName = "price";
                    labelKey = "Search.Facet.Price";
                    displayOrder = _searchSettings.PriceDisplayOrder;
                    break;
                case FacetGroupKind.Rating:
                    if (_searchSettings.RatingDisabled)
                        return;

                    fieldName = "rating";
                    labelKey = "Search.Facet.Rating";
                    displayOrder = _searchSettings.RatingDisplayOrder;
                    break;
                case FacetGroupKind.DeliveryTime:
                    if (_searchSettings.DeliveryTimeDisabled)
                        return;

                    fieldName = "deliveryid";
                    labelKey = "Search.Facet.DeliveryTime";
                    displayOrder = _searchSettings.DeliveryTimeDisplayOrder;
                    break;
                case FacetGroupKind.Availability:
                    if (_searchSettings.AvailabilityDisabled)
                        return;

                    fieldName = "available";
                    labelKey = "Search.Facet.Availability";
                    displayOrder = _searchSettings.AvailabilityDisplayOrder;
                    break;
                case FacetGroupKind.NewArrivals:
                    if (_searchSettings.NewArrivalsDisabled)
                        return;

                    fieldName = "createdon";
                    labelKey = "Search.Facet.NewArrivals";
                    displayOrder = _searchSettings.NewArrivalsDisplayOrder;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown field name for facet group '{kind}'");
            }

            var descriptor = new FacetDescriptor(fieldName)
            {
                Label = _services.Localization.GetResource(labelKey, returnEmptyIfNotFound: true).NullEmpty() ?? kind.ToString(),
                IsMultiSelect = isMultiSelect,
                DisplayOrder = displayOrder,
                OrderBy = sorting,
                MinHitCount = _searchSettings.FilterMinHitCount,
                MaxChoicesCount = _searchSettings.FilterMaxChoicesCount
            };

            addValues(descriptor);
            query.WithFacet(descriptor);
        }

        protected virtual void ConvertCategory(CatalogSearchQuery query, string origin)
        {
            if (origin.EqualsNoCase("Catalog/Category"))
            {
                // We don't need category facetting in category pages.
                return;
            }

            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Category, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "c", out List<int> ids) && ids != null && ids.Count > 0)
            {
                // TODO; (mc) Get deep ids (???) Make a low-level version of CatalogHelper.GetChildCategoryIds()
                query.WithCategoryIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? null : false, ids.ToArray());
            }

            AddFacet(query, FacetGroupKind.Category, true, FacetSorting.HitsDesc, descriptor =>
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        descriptor.AddValue(new FacetValue(id, IndexTypeCode.Int32)
                        {
                            IsSelected = true
                        });
                    }
                }
            });
        }

        protected virtual void ConvertManufacturer(CatalogSearchQuery query, string origin)
        {
            //List<int> ids = null;
            //int? minHitCount = null;

            //GetValueFor(query, "m", FacetGroupKind.Brand, out ids);

            //// Preselect manufacturer on manufacturer page.... and then?
            //if (origin.IsCaseInsensitiveEqual("Catalog/Manufacturer"))
            //{
            //	minHitCount = 0;

            //	var manufacturerId = routeData.Values["manufacturerid"].ToString().ToInt();
            //	if (manufacturerId != 0)
            //	{
            //		if (ids == null)
            //			ids = new List<int> { manufacturerId };
            //		else if (!ids.Contains(manufacturerId))
            //			ids.Add(manufacturerId);
            //	}
            //}

            if (origin.EqualsNoCase("Catalog/Manufacturer"))
            {
                // We don't need brand facetting in brand pages.
                return;
            }

            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Brand, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "m", out List<int> ids) && ids != null && ids.Count > 0)
            {
                query.WithManufacturerIds(null, ids.ToArray());
            }

            AddFacet(query, FacetGroupKind.Brand, true, FacetSorting.LabelAsc, descriptor =>
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        descriptor.AddValue(new FacetValue(id, IndexTypeCode.Int32)
                        {
                            IsSelected = true
                        });
                    }
                }
            });
        }

        protected virtual void ConvertPrice(CatalogSearchQuery query, string origin)
        {
            double? minPrice = null;
            double? maxPrice = null;
            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Price, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "p", out string price) && TryParseRange(price, out minPrice, out maxPrice))
            {
                // TODO: (mc) Why the heck did I convert this??!!
                //var currency = _services.WorkContext.WorkingCurrency;

                //if (minPrice.HasValue)
                //{
                //    minPrice = _currencyService.ConvertToPrimaryStoreCurrency(minPrice.Value, currency);
                //}

                //if (maxPrice.HasValue)
                //{
                //    maxPrice = _currencyService.ConvertToPrimaryStoreCurrency(maxPrice.Value, currency);
                //}

                // Normalization.
                if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                {
                    var tmp = minPrice;
                    minPrice = maxPrice;
                    maxPrice = tmp;
                }

                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    query.PriceBetween(
                        minPrice.HasValue ? (decimal)minPrice.Value : null,
                        maxPrice.HasValue ? (decimal)maxPrice.Value : null);
                }
            }

            AddFacet(query, FacetGroupKind.Price, false, FacetSorting.DisplayOrder, descriptor =>
            {
                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    descriptor.AddValue(new FacetValue(
                        minPrice,
                        maxPrice,
                        IndexTypeCode.Double,
                        minPrice.HasValue,
                        maxPrice.HasValue)
                    {
                        IsSelected = true
                    });
                }
            });
        }

        protected virtual void ConvertRating(CatalogSearchQuery query, string origin)
        {
            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Rating, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "r", out double? fromRate) && fromRate.HasValue)
            {
                query.WithRating(fromRate, null);
            }

            AddFacet(query, FacetGroupKind.Rating, false, FacetSorting.DisplayOrder, descriptor =>
            {
                descriptor.MinHitCount = 0;
                descriptor.MaxChoicesCount = 5;

                if (fromRate.HasValue)
                {
                    descriptor.AddValue(new FacetValue(fromRate.Value, IndexTypeCode.Double)
                    {
                        IsSelected = true
                    });
                }
            });
        }

        protected virtual void ConvertAvailability(CatalogSearchQuery query, string origin)
        {
            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Availability, query.LanguageId ?? 0);
            TryGetValueFor(alias ?? "a", out bool availability);

            // Setting specifies the logical direction of the filter. That's smarter than just to specify a default value.
            if (_searchSettings.IncludeNotAvailable)
            {
                // False = show, True = hide unavailable products.
                if (availability)
                {
                    query.AvailableOnly(true);
                }
            }
            else
            {
                // False = hide, True = show unavailable products.
                if (!availability)
                {
                    query.AvailableOnly(true);
                }
            }

            AddFacet(query, FacetGroupKind.Availability, true, FacetSorting.LabelAsc, descriptor =>
            {
                descriptor.MinHitCount = 0;

                var newValue = availability
                    ? new FacetValue(true, IndexTypeCode.Boolean)
                    : new FacetValue(null, IndexTypeCode.Empty);

                newValue.IsSelected = availability;
                newValue.Label = _services.Localization.GetResource(_searchSettings.IncludeNotAvailable ? "Search.Facet.ExcludeOutOfStock" : "Search.Facet.IncludeOutOfStock");

                descriptor.AddValue(newValue);
            });
        }

        protected virtual void ConvertNewArrivals(CatalogSearchQuery query, string origin)
        {
            var newForMaxDays = _catalogSettings.LabelAsNewForMaxDays ?? 0;
            if (newForMaxDays <= 0)
            {
                // We cannot filter without it.
                return;
            }

            var fromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(newForMaxDays));
            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.NewArrivals, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "n", out bool newArrivalsOnly) && newArrivalsOnly)
            {
                query.CreatedBetween(fromUtc, null);
            }

            AddFacet(query, FacetGroupKind.NewArrivals, true, FacetSorting.LabelAsc, descriptor =>
            {
                var label = _services.Localization.GetResource("Search.Facet.LastDays");

                descriptor.AddValue(new FacetValue(fromUtc, null, IndexTypeCode.DateTime, true, false)
                {
                    IsSelected = newArrivalsOnly,
                    Label = label.FormatInvariant(newForMaxDays)
                });
            });
        }

        protected virtual void ConvertDeliveryTime(CatalogSearchQuery query, string origin)
        {
            var alias = _catalogSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.DeliveryTime, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "d", out List<int> ids) && ids != null && ids.Count > 0)
            {
                query.WithDeliveryTimeIds(ids.ToArray());
            }

            AddFacet(query, FacetGroupKind.DeliveryTime, true, FacetSorting.DisplayOrder, descriptor =>
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        descriptor.AddValue(new FacetValue(id, IndexTypeCode.Int32)
                        {
                            IsSelected = true
                        });
                    }
                }
            });
        }

        protected virtual Task OnConvertedAsync(CatalogSearchQuery query, string origin)
        {
            return Task.CompletedTask;
        }
    }
}
