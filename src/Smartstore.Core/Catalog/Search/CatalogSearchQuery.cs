using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    [ModelBinder(typeof(CatalogSearchQueryModelBinder))]
    [ValidateNever]
    public partial class CatalogSearchQuery : SearchQuery<CatalogSearchQuery>, ICloneable<CatalogSearchQuery>
    {
        private readonly static Func<DbSet<Product>, int[], Task<List<Product>>> _defaultHitsFactory = (dbSet, ids) 
            => dbSet.GetManyAsync(ids);

        private Func<DbSet<Product>, int[], Task<List<Product>>> _hitsFactory = _defaultHitsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogSearchQuery"/> class without a search term being set
        /// </summary>
        public CatalogSearchQuery()
            : base((string[])null, null)
        {
        }

        public CatalogSearchQuery(string field, string term, SearchMode mode = SearchMode.Contains, bool escape = true, bool isFuzzySearch = false)
            : base(field.HasValue() ? new[] { field } : null, term, mode, escape, isFuzzySearch)
        {
        }

        public CatalogSearchQuery(string[] fields, string term, SearchMode mode = SearchMode.Contains, bool escape = true, bool isFuzzySearch = false)
            : base(fields, term, mode, escape, isFuzzySearch)
        {
        }

        public CatalogSearchQuery Clone()
            => (CatalogSearchQuery)MemberwiseClone();

        object ICloneable.Clone()
            => MemberwiseClone();

        public bool IsSubPage
        {
            get
            {
                if (PageIndex > 0)
                {
                    return true;
                }

                var hasActiveFilter = FacetDescriptors.Values.Any(x => x.Values.Any(y => y.IsSelected));
                return hasActiveFilter;
            }
        }

        // Using Func<> properties in bindable models significantly reduces response time
        // due to a "bug" in the MVC model binding/validation system: https://github.com/dotnet/aspnetcore/issues/27709
        public Func<DbSet<Product>, int[], Task<List<Product>>> GetHitsFactory()
            => _hitsFactory;

        /// <summary>
        /// Uses the given factory to load products from database AFTER all matching product ids has been determined.
        /// Gives you the chance - among other things - to eager load navigation properties.
        /// </summary>
        /// <param name="hitsFactory">The factory to use. The second param contains all matched product ids.</param>
        public CatalogSearchQuery UseHitsFactory(Func<DbSet<Product>, int[], Task<List<Product>>> hitsFactory)
        {
            _hitsFactory = hitsFactory;
            return this;
        }

        public CatalogSearchQuery SortBy(ProductSortingEnum sort)
        {
            switch (sort)
            {
                case ProductSortingEnum.CreatedOnAsc:
                case ProductSortingEnum.CreatedOn:
                    return SortBy(SearchSort.ByDateTimeField("createdon", sort == ProductSortingEnum.CreatedOn));

                case ProductSortingEnum.NameAsc:
                case ProductSortingEnum.NameDesc:
                    return SortBy(SearchSort.ByStringField("name", sort == ProductSortingEnum.NameDesc));

                case ProductSortingEnum.PriceAsc:
                case ProductSortingEnum.PriceDesc:
                    return SortBy(SearchSort.ByDoubleField("price", sort == ProductSortingEnum.PriceDesc));

                case ProductSortingEnum.Relevance:
                    return SortBy(SearchSort.ByRelevance());

                case ProductSortingEnum.Initial:
                default:
                    return this;
            }
        }

        /// <summary>
        /// Only products that are visible in frontend.
        /// </summary>
        /// <param name="customer">Customer whose customer roles are to be checked. Can be <c>null</c>.</param>
        /// <returns>Catalog search query</returns>
        public CatalogSearchQuery VisibleOnly(Customer customer)
        {
            if (customer != null)
            {
                var allowedCustomerRoleIds = customer.GetRoleIds();

                return VisibleOnly(allowedCustomerRoleIds);
            }

            return VisibleOnly(Array.Empty<int>());
        }

        /// <summary>
        /// Only products that are visible in frontend.
        /// </summary>
        /// <param name="allowedCustomerRoleIds">List of allowed customer role ids. Can be <c>null</c>.</param>
        public CatalogSearchQuery VisibleOnly(params int[] allowedCustomerRoleIds)
        {
            PublishedOnly(true);
            AvailableByDate(true);
            AllowedCustomerRoles(allowedCustomerRoleIds);

            return this;
        }

        public CatalogSearchQuery AllowedCustomerRoles(params int[] customerRoleIds)
        {
            if (customerRoleIds != null && customerRoleIds.Any())
            {
                var roleIds = customerRoleIds.Where(x => x != 0).Distinct().ToList();
                if (roleIds.Any())
                {
                    roleIds.Insert(0, 0);
                    return WithFilter(SearchFilter.Combined(roleIds.Select(x => SearchFilter.ByField("roleid", x).ExactMatch().NotAnalyzed()).ToArray()));
                }
            }

            return this;
        }

        public CatalogSearchQuery PublishedOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("published", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filters products based on their stock level.
        /// </summary>
        public CatalogSearchQuery AvailableOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("available", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filters products by their availability date.
        /// </summary>
        public CatalogSearchQuery AvailableByDate(bool value)
        {
            var utcNow = DateTime.UtcNow;

            if (value)
            {
                WithFilter(SearchFilter.ByRange("availablestart", null, utcNow, false, false).Mandatory().NotAnalyzed());
                WithFilter(SearchFilter.ByRange("availableend", utcNow, null, false, false).Mandatory().NotAnalyzed());
            }
            else
            {
                WithFilter(SearchFilter.ByRange("availablestart", utcNow, null, false, false).Mandatory().NotAnalyzed());
                WithFilter(SearchFilter.ByRange("availableend", null, utcNow, false, false).Mandatory().NotAnalyzed());
            }

            return this;
        }

        public CatalogSearchQuery WithVisibility(ProductVisibility value)
        {
            return WithFilter(SearchFilter.ByField("visibility", (int)value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery HasParentGroupedProduct(params int[] parentProductIds)
        {
            return CreateFilter("parentid", parentProductIds);
        }

        public override CatalogSearchQuery HasStoreId(int id)
        {
            base.HasStoreId(id);

            if (id == 0)
            {
                // 0 is ignored in queries, i.e. no filtering takes place. 
                // This should be kept here so that search engines do not provide different results.
                //WithFilter(SearchFilter.ByField("storeid", 0).ExactMatch().NotAnalyzed());
            }
            else
            {
                WithFilter(SearchFilter.Combined(
                    SearchFilter.ByField("storeid", 0).ExactMatch().NotAnalyzed(),
                    SearchFilter.ByField("storeid", id).ExactMatch().NotAnalyzed())
                );
            }

            return this;
        }

        public CatalogSearchQuery IsProductType(ProductType type)
        {
            return WithFilter(SearchFilter.ByField("typeid", (int)type).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery WithProductIds(params int[] ids)
        {
            return CreateFilter("id", ids);
        }

        public CatalogSearchQuery WithProductId(int? fromId, int? toId)
        {
            if (fromId == null && toId == null)
            {
                return this;
            }

            return WithFilter(SearchFilter.ByRange("id", fromId, toId, fromId.HasValue, toId.HasValue).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filter by category identifiers.
        /// </summary>
        /// <param name="featuredOnly">
        /// A value indicating whether loaded products are marked as featured (relates only to categories and manufacturers). 
        /// <c>false</c> to load featured products only, <c>true</c> to load unfeatured products only, <c>null</c> to load all products.
        /// </param>
        /// <param name="ids">The category identifiers.</param>
        /// <returns>Search query.</returns>
        public CatalogSearchQuery WithCategoryIds(bool? featuredOnly, params int[] ids)
        {
            var fieldName = featuredOnly.HasValue
                ? featuredOnly.Value ? "featuredcategoryid" : "notfeaturedcategoryid"
                : "categoryid";

            return CreateFilter(fieldName, ids);
        }

        /// <remarks>Includes only published categories.</remarks>
        public CatalogSearchQuery HasAnyCategory(bool value)
        {
            if (value)
            {
                return WithFilter(SearchFilter.ByRange("categoryid", 1, int.MaxValue, true, true).Mandatory().NotAnalyzed());
            }
            else
            {
                return WithFilter(SearchFilter.ByField("categoryid", 0).Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery WithManufacturerIds(bool? featuredOnly, params int[] ids)
        {
            var fieldName = featuredOnly.HasValue
                ? featuredOnly.Value ? "featuredmanufacturerid" : "notfeaturedmanufacturerid"
                : "manufacturerid";

            return CreateFilter(fieldName, ids);
        }

        /// <remarks>Includes only published manufacturers.</remarks>
        public CatalogSearchQuery HasAnyManufacturer(bool value)
        {
            if (value)
            {
                return WithFilter(SearchFilter.ByRange("manufacturerid", 1, int.MaxValue, true, true).Mandatory().NotAnalyzed());
            }
            else
            {
                return WithFilter(SearchFilter.ByField("manufacturerid", 0).Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery WithProductTagIds(params int[] ids)
        {
            return CreateFilter("tagid", ids);
        }

        public CatalogSearchQuery WithDeliveryTimeIds(params int[] ids)
        {
            return CreateFilter("deliveryid", ids);
        }

        public CatalogSearchQuery WithCondition(params ProductCondition[] conditions)
        {
            var len = conditions?.Length ?? 0;
            if (len > 0)
            {
                if (len == 1)
                {
                    return WithFilter(SearchFilter.ByField("condition", (int)conditions[0]).Mandatory().ExactMatch().NotAnalyzed());
                }

                return WithFilter(SearchFilter.Combined(conditions.Select(x => SearchFilter.ByField("condition", (int)x).ExactMatch().NotAnalyzed()).ToArray()));
            }

            return this;
        }

        public CatalogSearchQuery HomePageProductsOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("showonhomepage", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery DownloadOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("download", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery RecurringOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("recurring", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery ShipEnabledOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("shipenabled", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery FreeShippingOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("shipfree", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery TaxExemptOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("taxexempt", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery EsdOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("esd", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery HasDiscount(bool value)
        {
            return WithFilter(SearchFilter.ByField("discount", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery WithStockQuantity(
            int? fromQuantity,
            int? toQuantity,
            bool? includeFrom = null,
            bool? includeTo = null)
        {
            if (fromQuantity == null && toQuantity == null)
            {
                return this;
            }

            if (fromQuantity.HasValue && toQuantity.HasValue && fromQuantity == toQuantity)
            {
                var forbidden = includeFrom.HasValue && includeTo.HasValue && !includeFrom.Value && !includeTo.Value;

                return WithFilter(SearchFilter.ByField("stockquantity", fromQuantity.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    "stockquantity",
                    fromQuantity,
                    toQuantity,
                    includeFrom ?? fromQuantity.HasValue,
                    includeTo ?? toQuantity.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery PriceBetween(
            Money? fromPrice,
            Money? toPrice,
            bool? includeFrom = null,
            bool? includeTo = null)
        {
            if (fromPrice == null && toPrice == null)
            {
                return this;
            }

            Guard.NotEmpty(CurrencyCode, nameof(CurrencyCode));

            var fieldName = "price_c-" + CurrencyCode.EmptyNull().ToLower();

            if (fromPrice.HasValue && toPrice.HasValue && fromPrice == toPrice)
            {
                var forbidden = includeFrom.HasValue && includeTo.HasValue && !includeFrom.Value && !includeTo.Value;

                return WithFilter(SearchFilter
                    .ByField(fieldName, decimal.ToDouble(fromPrice.Value.Amount))
                    .Mandatory(!forbidden)
                    .ExactMatch()
                    .NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(fieldName,
                    fromPrice.HasValue ? decimal.ToDouble(fromPrice.Value.Amount) : null,
                    toPrice.HasValue ? decimal.ToDouble(toPrice.Value.Amount) : null,
                    includeFrom ?? fromPrice.HasValue,
                    includeTo ?? toPrice.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery CreatedBetween(
            DateTime? fromUtc,
            DateTime? toUtc,
            bool? includeFrom = null,
            bool? includeTo = null)
        {
            if (fromUtc == null && toUtc == null)
            {
                return this;
            }

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc == toUtc)
            {
                var forbidden = includeFrom.HasValue && includeTo.HasValue && !includeFrom.Value && !includeTo.Value;

                return WithFilter(SearchFilter.ByField("createdon", fromUtc.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    "createdon",
                    fromUtc,
                    toUtc,
                    includeFrom ?? fromUtc.HasValue,
                    includeTo ?? toUtc.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery WithRating(
            double? fromRate,
            double? toRate,
            bool? includeFrom = null,
            bool? includeTo = null)
        {
            if (fromRate == null && toRate == null)
            {
                return this;
            }
            if (fromRate.HasValue)
            {
                Guard.InRange(fromRate.Value, 0.0, 5.0, nameof(fromRate.Value));
            }
            if (toRate.HasValue)
            {
                Guard.InRange(toRate.Value, 0.0, 5.0, nameof(toRate.Value));
            }

            if (fromRate.HasValue && toRate.HasValue && fromRate == toRate)
            {
                var forbidden = includeFrom.HasValue && includeTo.HasValue && !includeFrom.Value && !includeTo.Value;

                return WithFilter(SearchFilter.ByField("rating", fromRate.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    "rating",
                    fromRate,
                    toRate,
                    includeFrom ?? fromRate.HasValue,
                    includeTo ?? toRate.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }
    }
}
