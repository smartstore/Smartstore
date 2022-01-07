using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Products.Rules
{
    public partial class ProductRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly SearchSettings _searchSettings;

        public ProductRuleOptionsProvider(
            SmartDbContext db,
            IWorkContext workContext,
            ICatalogSearchService catalogSearchService,
            ILocalizedEntityService localizedEntityService,
            SearchSettings searchSettings)
        {
            _db = db;
            _workContext = workContext;
            _catalogSearchService = catalogSearchService;
            _localizedEntityService = localizedEntityService;
            _searchSettings = searchSettings;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.Product;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.Product)
            {
                if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
                {
                    var products = await _db.Products.GetManyAsync(context.Value.ToIntArray());

                    result.AddOptions(context, products.Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false),
                        Hint = x.Sku
                    }));
                }
                else
                {
                    var options = await SearchProducts(result, context.SearchTerm, context.PageIndex * context.PageSize, context.PageSize);
                    result.AddOptions(context, options);
                    result.IsPaged = true;
                }
            }
            else
            {
                return null;
            }

            return result;
        }

        private async Task<List<RuleValueSelectListOption>> SearchProducts(RuleOptionsResult result, string term, int skip, int take)
        {
            IEnumerable<Product> products = null;
            var localeKeyGroup = nameof(Product);
            var localeKey = nameof(Product.Name);
            var languageId = _workContext.WorkingLanguage.Id;
            var fields = new List<string> { "name" };

            if (_searchSettings.SearchFields.Contains("sku"))
            {
                fields.Add("sku");
            }
            if (_searchSettings.SearchFields.Contains("shortdescription"))
            {
                fields.Add("shortdescription");
            }

            var searchQuery = new CatalogSearchQuery(fields.ToArray(), term);

            if (_searchSettings.UseCatalogSearchInBackend)
            {
                searchQuery = searchQuery
                    .Slice(skip, take)
                    .SortBy(ProductSortingEnum.NameAsc);

                var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                var hits = await searchResult.GetHitsAsync();

                result.HasMoreData = hits.HasNextPage;
                products = hits;
            }
            else
            {
                var query = _catalogSearchService.PrepareQuery(searchQuery);

                var pageIndex = take == 0 ? 0 : Math.Max(skip / take, 0);
                result.HasMoreData = (pageIndex + 1) * take < query.Count();

                products = await query
                    .Select(x => new Product
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Sku = x.Sku
                    })
                    .OrderBy(x => x.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }

            await _localizedEntityService.PrefetchLocalizedPropertiesAsync(localeKeyGroup, languageId, products.Select(x => x.Id).ToArray());

            var options = products
                .Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = _localizedEntityService.GetLocalizedValue(languageId, x.Id, localeKeyGroup, localeKey).NullEmpty() ?? x.Name,
                    Hint = x.Sku
                })
                .ToList();

            return options;
        }
    }
}
