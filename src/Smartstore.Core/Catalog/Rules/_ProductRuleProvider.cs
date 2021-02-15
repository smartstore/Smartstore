using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Rules
{
    public partial class ProductRuleProvider : RuleProviderBase, IProductRuleProvider
    {
        private readonly IRuleService _ruleService;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICategoryService _categoryService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly CatalogSettings _catalogSettings;

        public ProductRuleProvider(
            IRuleService ruleService,
            ICatalogSearchService catalogSearchService,
            ICategoryService categoryService,
            IWorkContext workContext, 
            IStoreContext storeContext,
            CatalogSettings catalogSettings)
            : base(RuleScope.Product)
        {
            _ruleService = ruleService;
            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _workContext = workContext;
            _storeContext = storeContext;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<SearchFilterExpressionGroup> CreateExpressionGroupAsync(int ruleSetId)
        {
            return await _ruleService.CreateExpressionGroupAsync(ruleSetId, this) as SearchFilterExpressionGroup;
        }

        public override IRuleExpression VisitRule(RuleEntity rule)
        {
            var expression = new SearchFilterExpression();
            base.ConvertRule(rule, expression);
            expression.Descriptor = ((RuleExpression)expression).Descriptor as SearchFilterDescriptor;
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            var group = new SearchFilterExpressionGroup
            {
                Id = ruleSet.Id,
                LogicalOperator = ruleSet.LogicalOperator,
                IsSubGroup = ruleSet.IsSubGroup,
                Value = ruleSet.Id,
                RawValue = ruleSet.Id.ToString(),
                Provider = this
            };

            return group;
        }

        public async Task<CatalogSearchResult> SearchAsync(SearchFilterExpression[] filters, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var searchQuery = new CatalogSearchQuery()
                .OriginatesFrom("Rule/Search")
                .WithLanguage(_workContext.WorkingLanguage)
                .WithCurrency(_workContext.WorkingCurrency)
                .BuildFacetMap(false)
                .CheckSpelling(0)
                .Slice(pageIndex * pageSize, pageSize)
                .SortBy(ProductSortingEnum.CreatedOn);

            if ((filters?.Length ?? 0) == 0)
            {
                return new CatalogSearchResult(searchQuery);
            }

            SearchFilterExpressionGroup group;

            if (filters.Length == 1 && filters[0] is SearchFilterExpressionGroup group2)
            {
                group = group2;
            }
            else
            {
                group = new SearchFilterExpressionGroup();
                group.AddExpressions(filters);
            }

            searchQuery = group.ApplyFilters(searchQuery);

            var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
            return searchResult;
        }

        protected override IEnumerable<RuleDescriptor> LoadDescriptors()
        {
            var language = _workContext.WorkingLanguage;
            var oneStarStr = T("Search.Facet.1StarAndMore").Value;
            var xStarsStr = T("Search.Facet.XStarsAndMore").Value;

            var stores = _storeContext.GetAllStores()
                .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                .ToArray();

            var visibilities = ((ProductVisibility[])Enum.GetValues(typeof(ProductVisibility)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = x.GetLocalizedEnum() })
                .ToArray();

            var productTypes = ((ProductType[])Enum.GetValues(typeof(ProductType)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = x.GetLocalizedEnum() })
                .ToArray();

            var ratings = FacetUtility.GetRatings()
                .Reverse()
                .Skip(1)
                .Select(x => new RuleValueSelectListOption
                {
                    Value = ((double)x.Value).ToString(CultureInfo.InvariantCulture),
                    Text = (double)x.Value == 1 ? oneStarStr : xStarsStr.FormatInvariant(x.Value)
                })
                .ToArray();

            #region Special filters

            // TODO: (mg) (core) Check whether RuleProviderBase.LoadDescriptors and related stuff needs to be async.
            //CatalogSearchQuery categoryFilter(SearchFilterContext ctx, int[] x)
            //{
            //    if (x?.Any() ?? false)
            //    {
            //        var ids = new HashSet<int>(x);

            //        if (_catalogSettings.ShowProductsFromSubcategories)
            //        {
            //            var tree = _categoryService.GetCategoryTreeAsync(includeHidden: true);

            //            foreach (var id in x)
            //            {
            //                var node = tree.SelectNodeById(id);
            //                if (node != null)
            //                {
            //                    ids.AddRange(node.Flatten(false).Select(y => y.Id));
            //                }
            //            }
            //        }

            //        return ctx.Query.WithCategoryIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? (bool?)null : false, ids.ToArray());
            //    }

            //    return ctx.Query;
            //};

            #endregion

            var descriptors = new List<SearchFilterDescriptor>
            {
            };


            descriptors
                .Where(x => x.RuleType == RuleType.Money)
                .Each(x => x.Metadata["postfix"] = _storeContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode);

            return descriptors;
        }
    }
}
