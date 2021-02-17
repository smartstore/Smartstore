using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly SearchSettings _searchSettings;

        public ProductRuleOptionsProvider(
            SmartDbContext db,
            ICatalogSearchService catalogSearchService,
            SearchSettings searchSettings)
        {
            _db = db;
            _catalogSearchService = catalogSearchService;
            _searchSettings = searchSettings;
        }

        public int Ordinal => 0;

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
            List<RuleValueSelectListOption> products;
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

                products = hits
                    .AsQueryable()
                    .Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name,
                        Hint = x.Sku
                    })
                    .ToList();
            }
            else
            {
                var query = _catalogSearchService.PrepareQuery(searchQuery);

                var pageIndex = take == 0 ? 0 : Math.Max(skip / take, 0);
                result.HasMoreData = (pageIndex + 1) * take < query.Count();

                products = await query
                    .Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name,
                        Hint = x.Sku
                    })
                    .OrderBy(x => x.Text)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }

            return products;
        }
    }
}
