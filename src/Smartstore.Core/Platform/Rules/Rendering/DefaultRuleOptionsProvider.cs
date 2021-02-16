using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Shipping;
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
        protected readonly Lazy<IProviderManager> _providerManager;
        protected readonly SearchSettings _searchSettings;

        public DefaultRuleOptionsProvider(
            ICommonServices services,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICategoryService> categoryService,
            Lazy<IProviderManager> providerManager,
            SearchSettings searchSettings)
        {
            _services = services;
            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _providerManager = providerManager;
            _searchSettings = searchSettings;
        }

        public bool Matches(string dataSource)
        {
            // TODO: (mg) (core) Make a static helper class for known data sources, e.g. "KnownRuleOptionDataSourceNames"
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

            if (descriptor.SelectList is not RemoteRuleValueSelectList list)
            {
                return result;
            }

            var db = _services.DbContext;
            var language = _services.WorkContext.WorkingLanguage;
            var byId = descriptor.RuleType == RuleType.Int || descriptor.RuleType == RuleType.IntArray;
            List<RuleValueSelectListOption> options = null;

            // TODO: (mg) (core) This is (and always was) way too monolithic. Split this monter into many option provider classes:
            // E.g.: CommonRuleOptionsProvider, Product..., Category... etc. Put classes to "OptionProviders" subfolder.
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
                        var categories = await _categoryService.Value.GetCategoryTreeAsync(0, true);

                        options = await categories
                            .Flatten(false)
                            .SelectAsync(async x => new RuleValueSelectListOption
                            { 
                                Value = x.Id.ToString(),
                                Text = (await _categoryService.Value.GetCategoryPathAsync(x, language.Id)).NullEmpty() ?? x.Name
                            })
                            .ToListAsync();
                    }
                    break;
                case "Manufacturer":
                    var manufacturers = await db.Manufacturers
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    options = manufacturers
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false) })
                        .ToList();
                    break;
                case "PaymentMethod":
                    // TODO: (mg) (core) Complete DefaultRuleOptionsProvider (IPaymentMethod required).
                    //options = await _providerManager.Value.GetAllProviders<IPaymentMethod>()
                    //    .Select(x => x.Metadata)
                    //    .SelectAsync(async x => new RuleValueSelectListOption
                    //    {
                    //        Value = x.SystemName,
                    //        Text = await GetLocalizedAsync(x, "FriendlyName") ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                    //        Hint = x.SystemName
                    //    })
                    //    .ToListAsync();
                    //options = options.OrderBy(x => x.Text).ToList();
                    break;
                case "ShippingRateComputationMethod":
                    options = await _providerManager.Value.GetAllProviders<IShippingRateComputationMethod>()
                        .Select(x => x.Metadata)
                        .SelectAsync(async x => new RuleValueSelectListOption 
                        {
                            Value = x.SystemName, 
                            Text = await GetLocalizedAsync(x, "FriendlyName") ?? x.FriendlyName.NullEmpty() ?? x.SystemName, 
                            Hint = x.SystemName
                        })
                        .ToListAsync();
                    options = options.OrderBy(x => x.Text).ToList();
                    break;
                case "ShippingMethod":
                    var shippingMethods = await db.ShippingMethods
                        .AsNoTracking()
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    options = shippingMethods
                        .Select(x => new RuleValueSelectListOption { Value = byId ? x.Id.ToString() : x.Name, Text = byId ? x.GetLocalized(y => y.Name, language, true, false) : x.Name })
                        .ToList();
                    break;
                case "ProductTag":
                    var productTags = await db.ProductTags.AsNoTracking().ToListAsync();

                    options = productTags
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false) })
                        .OrderBy(x => x.Text)
                        .ToList();
                    break;
                case "VariantValue":
                    if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                    {
                        var variants = await db.ProductVariantAttributeValues.GetManyAsync(value.ToIntArray());

                        options = variants
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false) })
                            .ToList();
                    }
                    else if (descriptor.Metadata.TryGetValue("ParentId", out var objParentId))
                    {
                        options = new List<RuleValueSelectListOption>();
                        var pIndex = -1;
                        var existingValues = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                        var query = db.ProductVariantAttributeValues
                            .AsNoTracking()
                            .Where(x => x.ProductVariantAttribute.ProductAttributeId == (int)objParentId &&
                                x.ProductVariantAttribute.ProductAttribute.AllowFiltering &&
                                x.ValueTypeId == (int)ProductVariantAttributeValueType.Simple)
                            .ApplyValueFilter(null, true);

                        while (true)
                        {
                            var variants = await PagedList.Create(query, ++pIndex, 1000).LoadAsync();
                            foreach (var variant in variants)
                            {
                                var name = variant.GetLocalized(x => x.Name, language, true, false);
                                if (!existingValues.Contains(name))
                                {
                                    existingValues.Add(name);
                                    options.Add(new RuleValueSelectListOption { Value = variant.Id.ToString(), Text = name });
                                }
                            }
                            if (!variants.HasNextPage)
                            {
                                break;
                            }
                        }
                    }
                    break;
                case "AttributeOption":
                    if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                    {
                        var attributes = await db.SpecificationAttributeOptions.GetManyAsync(value.ToIntArray());

                        options = attributes
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false) })
                            .ToList();
                    }
                    else if (descriptor.Metadata.TryGetValue("ParentId", out var objParentId))
                    {
                        var attributes = await db.SpecificationAttributeOptions
                            .AsNoTracking()
                            .Where(x => x.SpecificationAttributeId == (int)objParentId)
                            .OrderBy(x => x.DisplayOrder)
                            .ToPagedList(pageIndex, pageSize)
                            .LoadAsync();

                        result.IsPaged = true;
                        result.HasMoreData = attributes.HasNextPage;

                        options = attributes
                            .AsQueryable()
                            .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(y => y.Name, language, true, false, false) })
                            .ToList();
                    }
                    break;
                default:
                    throw new SmartException($"Unknown data source '{list.DataSource.NaIfEmpty()}'.");
            }

            if (options != null)
            {
                if (reason == RuleOptionsRequestReason.SelectedDisplayNames)
                {
                    // Get display names of selected options.
                    if (value.HasValue())
                    {
                        var selectedValues = value.SplitSafe(",");
                        result.Options.AddRange(options.Where(x => selectedValues.Contains(x.Value)));
                    }
                }
                else
                {
                    // Get select list options.
                    if (!result.IsPaged && searchTerm.HasValue() && options.Any())
                    {
                        // Apply the search term if the options are not paged.
                        result.Options.AddRange(options.Where(x => (x.Text?.IndexOf(searchTerm, 0, StringComparison.CurrentCultureIgnoreCase) ?? -1) != -1));
                    }
                    else
                    {
                        result.Options.AddRange(options);
                    }
                }
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
