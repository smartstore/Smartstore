using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Rules.Rendering
{
    public partial class DefaultRuleOptionsProvider : IRuleOptionsProvider
    {
        protected readonly ICommonServices _services;
        protected readonly Lazy<ICatalogSearchService> _catalogSearchService;
        protected readonly Lazy<ICategoryService> _categoryService;
        protected readonly SearchSettings _searchSettings;

        public DefaultRuleOptionsProvider(
            ICommonServices services,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICategoryService> categoryService,
            SearchSettings searchSettings)
        {
            _services = services;
            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _searchSettings = searchSettings;
        }

        public bool Matches(string dataSource)
        {
            switch (dataSource.EmptyNull())
            {
                case "CartRule":
                case "Category":
                case "Country":
                case "Currency":
                case "DeliveryTime":
                case "CustomerRole":
                case "Language":
                case "Manufacturer":
                case "PaymentMethod":
                case "Product":
                case "ProductTag":
                case "ShippingMethod":
                case "ShippingRateComputationMethod":
                case "TargetGroup":
                case "VariantValue":
                case "AttributeOption":
                    return true;
                default:
                    return false;
            }
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(
            RuleOptionsRequestReason reason, 
            RuleDescriptor descriptor, 
            string value, 
            int pageIndex, 
            int pageSize, 
            string searchTerm)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var result = new RuleOptionsResult();

            if (!(descriptor.SelectList is RemoteRuleValueSelectList list))
            {
                return result;
            }

            var db = _services.DbContext;
            var language = _services.WorkContext.WorkingLanguage;
            var byId = descriptor.RuleType == RuleType.Int || descriptor.RuleType == RuleType.IntArray;
            List<RuleValueSelectListOption> options = null;

            switch (list.DataSource)
            {
                case "Product":
                    if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                    {
                        var products = await db.Products.GetManyAsync(value.ToIntArray());

                        options = products
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false), Hint = x.Sku })
                            .ToList();
                    }
                    else
                    {
                        result.IsPaged = true;
                        options = await SearchProductsAsync(result, searchTerm, pageIndex * pageSize, pageSize);
                    }
                    break;
                case "Country":
                    var countries = await db.Countries
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    options = countries
                        .Select(x => new RuleValueSelectListOption { Value = byId ? x.Id.ToString() : x.TwoLetterIsoCode, Text = x.GetLocalized(y => y.Name, language, true, false) })
                        .ToList();
                    break;
                case "Currency":
                    var currencies = await db.Currencies
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    options = currencies
                        .Select(x => new RuleValueSelectListOption { Value = byId ? x.Id.ToString() : x.CurrencyCode, Text = x.GetLocalized(y => y.Name, language, true, false) })
                        .ToList();
                    break;
                case "DeliveryTime":
                    var deliveryTimes = await db.DeliveryTimes
                        .AsNoTracking()
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    options = deliveryTimes
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false) })
                        .ToList();
                    break;
                case "CustomerRole":
                    var customerRoles = await db.CustomerRoles
                        .AsNoTracking()
                        .OrderBy(x => x.Name)
                        .ToListAsync();

                    options = customerRoles
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                        .ToList();
                    break;
                case "Language":
                    var languages = await db.Languages
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    options = languages
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = GetCultureDisplayName(x) ?? x.Name })
                        .ToList();
                    break;
                case "Store":
                    options = _services.StoreContext.GetAllStores()
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                        .ToList();
                    break;
                case "CartRule":
                case "TargetGroup":
                    if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                    {
                        var ruleSets = await db.RuleSets.GetManyAsync(value.ToIntArray());

                        options = ruleSets
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                            .ToList();
                    }
                    else
                    {
                        var ruleSets = await db.RuleSets
                            .AsNoTracking()
                            .ApplyStandardFilter(descriptor.Scope, false, true)
                            .ToPagedList(pageIndex, pageSize)
                            .LoadAsync();

                        result.IsPaged = true;
                        result.HasMoreData = ruleSets.HasNextPage;

                        options = ruleSets
                            .AsQueryable()
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                            .ToList();
                    }
                    break;
                case "Category":
                    if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                    {
                        var categories = await db.Categories.GetManyAsync(value.ToIntArray());

                        options = await categories
                            .SelectAsync(async x => new RuleValueSelectListOption
                            {
                                Value = x.Id.ToString(),
                                Text = (await _categoryService.Value.GetCategoryPathAsync(x, language.Id)).NullEmpty() ?? x.Name
                            })
                            .ToListAsync();
                    }
                    else
                    {
                    }
                    break;
            }

            return result;
        }

        protected virtual string GetCultureDisplayName(Language language)
        {
            if (language?.LanguageCulture?.HasValue() ?? false)
            {
                try
                {
                    return new CultureInfo(language.LanguageCulture).DisplayName;
                }
                catch { }
            }

            return null;
        }

        protected virtual async Task<string> GetLocalizedAsync(ProviderMetadata metadata, string propertyName)
        {
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            var resource = await _services.Localization.GetResourceAsync(resourceName, _services.WorkContext.WorkingLanguage.Id, false, "", true);

            return resource.NullEmpty();
        }

        protected virtual async Task<List<RuleValueSelectListOption>> SearchProductsAsync(RuleOptionsResult result, string term, int skip, int take)
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

                var searchResult = await _catalogSearchService.Value.SearchAsync(searchQuery);
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
                var query = _catalogSearchService.Value.PrepareQuery(searchQuery);

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
