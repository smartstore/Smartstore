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
            => dataSource == KnownRuleOptionDataSourceNames.Product;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.Product)
            {
                return null;
            }

            if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
            {
                var products = await _db.Products
                    .SelectSummary()
                    .GetManyAsync(context.Value.ToIntArray());

                var options = products.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false),
                    Hint = x.Sku
                });

                return RuleOptionsResult.Create(context, options);
            }
            else
            {
                return await SearchProducts(context);
            }
        }

        private async Task<RuleOptionsResult> SearchProducts(RuleOptionsContext context)
        {
            IEnumerable<Product> products = null;
            var localeKeyGroup = nameof(Product);
            var localeKey = nameof(Product.Name);
            var languageId = _workContext.WorkingLanguage.Id;
            var fields = new List<string> { "name" };
            var hasMoreData = false;
            var skip = context.PageIndex * context.PageSize;
            var take = context.PageSize;

            if (_searchSettings.SearchFields.Contains("sku"))
            {
                fields.Add("sku");
            }
            if (_searchSettings.SearchFields.Contains("shortdescription"))
            {
                fields.Add("shortdescription");
            }

            var searchQuery = new CatalogSearchQuery(fields.ToArray(), context.SearchTerm);

            if (_searchSettings.UseCatalogSearchInBackend)
            {
                searchQuery = searchQuery
                    .Slice(skip, take)
                    .SortBy(ProductSortingEnum.NameAsc);

                var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                var hits = await searchResult.GetHitsAsync();

                hasMoreData = hits.HasNextPage;
                products = hits;
            }
            else
            {
                var query = _catalogSearchService.PrepareQuery(searchQuery);

                var pageIndex = take == 0 ? 0 : Math.Max(skip / take, 0);
                hasMoreData = (pageIndex + 1) * take < query.Count();

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

            return RuleOptionsResult.Create(context, options, true, hasMoreData);
        }
    }
}
