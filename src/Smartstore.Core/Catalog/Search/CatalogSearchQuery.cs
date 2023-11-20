#nullable enable

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Identity;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class KnownFilterNames : IHideObjectMembers
    {
        internal KnownFilterNames() { }
        public readonly string ProductId = "id";
        public readonly string Name = "name";
        public readonly string Sku = "sku";
        public readonly string ShortDescription = "shortdescription";
        public readonly string CategoryId = "categoryid";
        public readonly string FeaturedCategoryId = "featuredcategoryid";
        public readonly string NotFeaturedCategoryId = "notfeaturedcategoryid";
        public readonly string ManufacturerId = "manufacturerid";
        public readonly string FeaturedManufacturerId = "featuredmanufacturerid";
        public readonly string NotFeaturedManufacturerId = "notfeaturedmanufacturerid";
        public readonly string CategoryPath = "categorypath";
        public readonly string FeaturedCategoryPath = "featuredcategorypath";
        public readonly string NotFeaturedCategoryPath = "notfeaturedcategorypath";
        public readonly string TagId = "tagid";
        public readonly string DeliveryId = "deliveryid";
        public readonly string ParentId = "parentid";
        public readonly string Condition = "condition";
        public readonly string StockQuantity = "stockquantity";
        public readonly string Rating = "rating";
        public readonly string CreatedOn = "createdon";
        public readonly string Price = "price";
        public readonly string IsPublished = "published";
        public readonly string Visibility = "visibility";
        public readonly string ShowOnHomepage = "showonhomepage";
        public readonly string IsDownload = "download";
        public readonly string IsRecurring = "recurring";
        public readonly string IsShippingEnabled = "shipenabled";
        public readonly string IsFreeShipping = "shipfree";
        public readonly string IsTaxExempt = "taxexempt";
        public readonly string IsEsd = "esd";
        public readonly string HasDiscount = "discount";
        public readonly string TypeId = "typeid";
        public readonly string StoreId = "storeid";
        public readonly string IsAvailable = "available";
        public readonly string AvailableStart = "availablestart";
        public readonly string AvailableEnd = "availableend";
        public readonly string RoleId = "roleid";
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class KnownSortingNames : IHideObjectMembers
    {
        internal KnownSortingNames() { }
        public readonly string Name = "name";
        public readonly string Price = "price";
        public readonly string CreatedOn = "createdon";
        public readonly string ParentId = "parentid";
    }

    [ValidateNever, ModelBinder(typeof(CatalogSearchQueryModelBinder))]
    public partial class CatalogSearchQuery : SearchQuery<CatalogSearchQuery>, ICloneable<CatalogSearchQuery>
    {
        public static KnownFilterNames KnownFilters = new();
        public static KnownSortingNames KnownSortings = new();

        private readonly static Func<DbSet<Product>, int[], Task<List<Product>>> _defaultHitsFactory = (dbSet, ids)
            => dbSet.SelectSummary().GetManyAsync(ids);

        private Func<DbSet<Product>, int[], Task<List<Product>>> _hitsFactory = _defaultHitsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogSearchQuery"/> class without a search term being set
        /// </summary>
        public CatalogSearchQuery()
            : base((string[])null!, null)
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

        /// <summary>
        /// Returns a new <see cref="CatalogSearchQuery"/> instance that is a memberwise copy of this query.
        /// </summary>
        /// <remarks>
        /// This creates a shallow copy of the original query! If you clear the filters of the clone, you also clear the filters of the original query.
        /// </remarks>
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
                    return SortBy(SearchSort.ByDateTimeField(KnownSortings.CreatedOn, sort == ProductSortingEnum.CreatedOn));

                case ProductSortingEnum.NameAsc:
                case ProductSortingEnum.NameDesc:
                    return SortBy(SearchSort.ByStringField(KnownSortings.Name, sort == ProductSortingEnum.NameDesc));

                case ProductSortingEnum.PriceAsc:
                case ProductSortingEnum.PriceDesc:
                    return SortBy(SearchSort.ByDoubleField(KnownSortings.Price, sort == ProductSortingEnum.PriceDesc));

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
                    return WithFilter(SearchFilter.Combined(
                        KnownFilters.RoleId,
                        roleIds.Select(x => SearchFilter.ByField(KnownFilters.RoleId, x).ExactMatch().NotAnalyzed()).ToArray()));
                }
            }

            return this;
        }

        public CatalogSearchQuery PublishedOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsPublished, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filters products based on their stock level.
        /// </summary>
        public CatalogSearchQuery AvailableOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsAvailable, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filters products by their availability date.
        /// </summary>
        public CatalogSearchQuery AvailableByDate(bool value)
        {
            var utcNow = DateTime.UtcNow;

            if (value)
            {
                WithFilter(SearchFilter.ByRange(KnownFilters.AvailableStart, null, utcNow, false, false).Mandatory().NotAnalyzed());
                WithFilter(SearchFilter.ByRange(KnownFilters.AvailableEnd, utcNow, null, false, false).Mandatory().NotAnalyzed());
            }
            else
            {
                WithFilter(SearchFilter.ByRange(KnownFilters.AvailableStart, utcNow, null, false, false).Mandatory().NotAnalyzed());
                WithFilter(SearchFilter.ByRange(KnownFilters.AvailableEnd, null, utcNow, false, false).Mandatory().NotAnalyzed());
            }

            return this;
        }

        public CatalogSearchQuery WithVisibility(ProductVisibility value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.Visibility, (int)value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery HasParentGroupedProduct(params int[] parentProductIds)
        {
            return CreateFilter(KnownFilters.ParentId, parentProductIds);
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
                WithFilter(SearchFilter.Combined(KnownFilters.StoreId,
                    SearchFilter.ByField(KnownFilters.StoreId, 0).ExactMatch().NotAnalyzed(),
                    SearchFilter.ByField(KnownFilters.StoreId, id).ExactMatch().NotAnalyzed())
                );
            }

            return this;
        }

        public CatalogSearchQuery IsProductType(ProductType type)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.TypeId, (int)type).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery WithProductIds(params int[] ids)
        {
            return CreateFilter(KnownFilters.ProductId, ids);
        }

        public CatalogSearchQuery WithProductId(int? fromId, int? toId)
        {
            if (fromId == null && toId == null)
            {
                return this;
            }

            return WithFilter(
                SearchFilter.ByRange(
                    KnownFilters.ProductId, fromId, toId, fromId.HasValue, toId.HasValue).Mandatory().ExactMatch().NotAnalyzed());
        }

        /// <summary>
        /// Filter products by category identifiers.
        /// For a large number of category IDs, it is recommended to use <see cref="WithCategoryTreePath(string, bool?, bool)"/> because it is faster.
        /// </summary>
        /// <param name="featuredOnly">
        /// A value indicating whether loaded products are marked as "featured" at their category assignment.
        /// <c>true</c> to load featured products only, <c>false</c> to load unfeatured products only, <c>null</c> to load all products.
        /// </param>
        /// <param name="ids">The category identifiers.</param>
        /// <returns>Search query.</returns>
        /// <remarks>
        /// Use <see cref="WithCategoryIds(bool?, int[])"/> or <see cref="WithCategoryTreePath(string, bool?, bool)"/>, but not both together.
        /// Both do the same thing (filter products based on their category assignments), just in different ways.
        /// </remarks>
        public CatalogSearchQuery WithCategoryIds(bool? featuredOnly, params int[] ids)
        {
            var fieldName = featuredOnly.HasValue
                ? featuredOnly.Value ? KnownFilters.FeaturedCategoryId : KnownFilters.NotFeaturedCategoryId
                : KnownFilters.CategoryId;

            return CreateFilter(fieldName, ids);
        }

        /// <summary>
        /// Filter products by category tree path.
        /// For a large number of category assignments, this method is faster than <see cref="WithCategoryIds(bool?, int[])"/>.
        /// </summary>
        /// <param name="treePath">The parent's tree path to get descendants from.</param>
        /// <param name="featuredOnly">
        /// A value indicating whether loaded products are marked as "featured" at their category assignment.
        /// <c>true</c> to load featured products only, <c>false</c> to load unfeatured products only, <c>null</c> to load all products.
        /// </param>
        /// <param name="includeSelf"><c>true</c> = add the parent node to the result list, <c>false</c> = ignore the parent node.</param>
        /// <returns>Search query.</returns>
        /// <remarks>
        /// Use <see cref="WithCategoryIds(bool?, int[])"/> or <see cref="WithCategoryTreePath(string, bool?, bool)"/>, but not both together.
        /// Both do the same thing (filter products based on their category assignments), just in different ways.
        /// </remarks>
        public CatalogSearchQuery WithCategoryTreePath(string treePath, bool? featuredOnly, bool includeSelf = true)
        {
            if (treePath.HasValue())
            {
                WithFilter(new CategoryTreePathFilter(treePath, featuredOnly, includeSelf));
            }

            return this;
        }

        /// <remarks>Includes only published categories.</remarks>
        public CatalogSearchQuery HasAnyCategory(bool value)
        {
            if (value)
            {
                return WithFilter(SearchFilter.ByRange(KnownFilters.CategoryId, 1, int.MaxValue, true, true).Mandatory().NotAnalyzed());
            }
            else
            {
                return WithFilter(SearchFilter.ByField(KnownFilters.CategoryId, 0).Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery WithManufacturerIds(bool? featuredOnly, params int[] ids)
        {
            var fieldName = featuredOnly.HasValue
                ? featuredOnly.Value ? KnownFilters.FeaturedManufacturerId : KnownFilters.NotFeaturedManufacturerId
                : KnownFilters.ManufacturerId;

            return CreateFilter(fieldName, ids);
        }

        /// <remarks>Includes only published manufacturers.</remarks>
        public CatalogSearchQuery HasAnyManufacturer(bool value)
        {
            if (value)
            {
                return WithFilter(SearchFilter.ByRange(KnownFilters.ManufacturerId, 1, int.MaxValue, true, true).Mandatory().NotAnalyzed());
            }
            else
            {
                return WithFilter(SearchFilter.ByField(KnownFilters.ManufacturerId, 0).Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery WithProductTagIds(params int[] ids)
        {
            return CreateFilter(KnownFilters.TagId, ids);
        }

        public CatalogSearchQuery WithDeliveryTimeIds(params int[] ids)
        {
            return CreateFilter(KnownFilters.DeliveryId, ids);
        }

        public CatalogSearchQuery WithCondition(params ProductCondition[] conditions)
        {
            var len = conditions?.Length ?? 0;
            if (len > 0)
            {
                if (len == 1)
                {
                    return WithFilter(SearchFilter.ByField(KnownFilters.Condition, (int)conditions![0]).Mandatory().ExactMatch().NotAnalyzed());
                }

                return WithFilter(
                    SearchFilter.Combined(
                        KnownFilters.Condition,
                        conditions!.Select(x => SearchFilter.ByField(KnownFilters.Condition, (int)x).ExactMatch().NotAnalyzed()).ToArray()));
            }

            return this;
        }

        public CatalogSearchQuery HomePageProductsOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.ShowOnHomepage, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery DownloadOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsDownload, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery RecurringOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsRecurring, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery ShipEnabledOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsShippingEnabled, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery FreeShippingOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsFreeShipping, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery TaxExemptOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsTaxExempt, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery EsdOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.IsEsd, value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public CatalogSearchQuery HasDiscount(bool value)
        {
            return WithFilter(SearchFilter.ByField(KnownFilters.HasDiscount, value).Mandatory().ExactMatch().NotAnalyzed());
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

                return WithFilter(SearchFilter.ByField(KnownFilters.StockQuantity, fromQuantity.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    KnownFilters.StockQuantity,
                    fromQuantity,
                    toQuantity,
                    includeFrom ?? fromQuantity.HasValue,
                    includeTo ?? toQuantity.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }

        public CatalogSearchQuery PriceBetween(
            decimal? fromPrice,
            decimal? toPrice,
            bool? includeFrom = null,
            bool? includeTo = null)
            => PriceBetween(null, fromPrice, toPrice, includeFrom, includeTo);

        public CatalogSearchQuery PriceBetween(
            string? currencyCode,
            decimal? fromPrice,
            decimal? toPrice,
            bool? includeFrom = null,
            bool? includeTo = null)
        {
            if (fromPrice == null && toPrice == null)
            {
                return this;
            }

            if (currencyCode.HasValue())
            {
                CurrencyCode = currencyCode;
            }

            if (CurrencyCode.IsEmpty())
            {
                throw new ArgumentException("A currency code is required to filter by price.", nameof(currencyCode));
            }

            var fieldName = "price_c-" + CurrencyCode.EmptyNull().ToLower();

            if (fromPrice.HasValue && toPrice.HasValue && fromPrice == toPrice)
            {
                var forbidden = includeFrom.HasValue && includeTo.HasValue && !includeFrom.Value && !includeTo.Value;

                return WithFilter(SearchFilter
                    .ByField(fieldName, decimal.ToDouble(fromPrice.Value))
                    .Mandatory(!forbidden)
                    .ExactMatch()
                    .NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(fieldName,
                    fromPrice.HasValue ? decimal.ToDouble(fromPrice.Value) : null,
                    toPrice.HasValue ? decimal.ToDouble(toPrice.Value) : null,
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

                return WithFilter(SearchFilter.ByField(KnownFilters.CreatedOn, fromUtc.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    KnownFilters.CreatedOn,
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

                return WithFilter(SearchFilter.ByField(KnownFilters.Rating, fromRate.Value).Mandatory(!forbidden).ExactMatch().NotAnalyzed());
            }
            else
            {
                var filter = SearchFilter.ByRange(
                    KnownFilters.Rating,
                    fromRate,
                    toRate,
                    includeFrom ?? fromRate.HasValue,
                    includeTo ?? toRate.HasValue);

                return WithFilter(filter.Mandatory().ExactMatch().NotAnalyzed());
            }
        }
    }
}
