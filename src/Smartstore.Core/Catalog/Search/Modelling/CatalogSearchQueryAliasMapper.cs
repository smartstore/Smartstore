using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Search.Modelling
{
    public partial class CatalogSearchQueryAliasMapper : ICatalogSearchQueryAliasMapper
    {
        private const string ALL_ATTRIBUTE_ID_BY_ALIAS_KEY = "search:attribute.id.alias.mappings.all";
        private const string ALL_ATTRIBUTE_ALIAS_BY_ID_KEY = "search:attribute.alias.id.mappings.all";
        private const string ALL_COMMONFACET_ALIAS_BY_KIND_KEY = "search:commonfacet.alias.kind.mappings.all";

        private const string ALL_VARIANT_ID_BY_ALIAS_KEY = "search:variant.id.alias.mappings.all";
        private const string ALL_VARIANT_ALIAS_BY_ID_KEY = "search:variant.alias.id.mappings.all";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly ISettingService _settingService;

        public CatalogSearchQueryAliasMapper(
            SmartDbContext db,
            ICacheManager cache,
            ISettingService settingService)
        {
            _db = db;
            _cache = cache;
            _settingService = settingService;
        }

        #region Specification Attributes

        public async Task<int> GetAttributeIdByAliasAsync(string attributeAlias, int languageId = 0)
        {
            var result = 0;

            if (attributeAlias.HasValue())
            {
                var mappings = await GetAttributeIdByAliasMappingsAsync();

                if (!mappings.TryGetValue(CreateKey("attr", languageId, attributeAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("attr", 0, attributeAlias), out result);
                }
            }

            return result;
        }

        public async Task<int> GetAttributeOptionIdByAliasAsync(string optionAlias, int attributeId, int languageId = 0)
        {
            var result = 0;

            if (optionAlias.HasValue() && attributeId != 0)
            {
                var mappings = await GetAttributeIdByAliasMappingsAsync();

                if (!mappings.TryGetValue(CreateOptionKey("attr.option", languageId, attributeId, optionAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("attr.option", 0, attributeId, optionAlias), out result);
                }
            }

            return result;
        }

        public async Task<string> GetAttributeAliasByIdAsync(int attributeId, int languageId = 0)
        {
            string result = null;

            if (attributeId != 0)
            {
                var mappings = await GetAttributeAliasByIdMappingsAsync();

                if (!mappings.TryGetValue(CreateKey("attr", languageId, attributeId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("attr", 0, attributeId), out result);
                }
            }

            return result;
        }

        public async Task<string> GetAttributeOptionAliasByIdAsync(int optionId, int languageId = 0)
        {
            string result = null;

            if (optionId != 0)
            {
                var mappings = await GetAttributeAliasByIdMappingsAsync();

                if (!mappings.TryGetValue(CreateOptionKey("attr.option", languageId, optionId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("attr.option", 0, optionId), out result);
                }
            }

            return result;
        }

        public async Task ClearAttributeCacheAsync()
        {
            await _cache.RemoveAsync(ALL_ATTRIBUTE_ID_BY_ALIAS_KEY);
            await _cache.RemoveAsync(ALL_ATTRIBUTE_ALIAS_BY_ID_KEY);
        }

        protected virtual async Task<IDictionary<string, int>> GetAttributeIdByAliasMappingsAsync()
        {
            var mappings = await _cache.GetAsync(ALL_ATTRIBUTE_ID_BY_ALIAS_KEY, async () =>
            {
                var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var optionIdMappings = new Dictionary<int, int>();

                var pager = _db.SpecificationAttributes
                    .AsNoTracking()
                    .Include(x => x.SpecificationAttributeOptions)
                    .OrderBy(x => x.Id)
                    .ToFastPager(500);

                while ((await pager.ReadNextPageAsync<SpecificationAttribute>()).Out(out var attributes))
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute.Alias.HasValue())
                        {
                            result[CreateKey("attr", 0, attribute.Alias)] = attribute.Id;
                        }

                        foreach (var option in attribute.SpecificationAttributeOptions)
                        {
                            optionIdMappings[option.Id] = option.SpecificationAttributeId;

                            if (option.Alias.HasValue())
                            {
                                result[CreateOptionKey("attr.option", 0, attribute.Id, option.Alias)] = option.Id;
                            }
                        }
                    }
                }

                await CacheLocalizedAliasAsync(nameof(SpecificationAttribute), x => result[CreateKey("attr", x.LanguageId, x.LocaleValue)] = x.EntityId);
                await CacheLocalizedAliasAsync(nameof(SpecificationAttributeOption), x =>
                {
                    if (optionIdMappings.TryGetValue(x.EntityId, out var attributeId))
                    {
                        result[CreateOptionKey("attr.option", x.LanguageId, attributeId, x.LocaleValue)] = x.EntityId;
                    }
                });

                return result;
            });

            return mappings;
        }

        protected virtual async Task<IDictionary<string, string>> GetAttributeAliasByIdMappingsAsync()
        {
            var mappings = await _cache.GetAsync(ALL_ATTRIBUTE_ALIAS_BY_ID_KEY, async () =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var pager = _db.SpecificationAttributes
                    .AsNoTracking()
                    .Include(x => x.SpecificationAttributeOptions)
                    .OrderBy(x => x.Id)
                    .ToFastPager(500);

                while ((await pager.ReadNextPageAsync<SpecificationAttribute>()).Out(out var attributes))
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute.Alias.HasValue())
                        {
                            result[CreateKey("attr", 0, attribute.Id)] = attribute.Alias;
                        }

                        foreach (var option in attribute.SpecificationAttributeOptions.Where(x => x.Alias.HasValue()))
                        {
                            result[CreateOptionKey("attr.option", 0, option.Id)] = option.Alias;
                        }
                    }
                }

                await CacheLocalizedAliasAsync(nameof(SpecificationAttribute), x => result[CreateKey("attr", x.LanguageId, x.EntityId)] = x.LocaleValue);
                await CacheLocalizedAliasAsync(nameof(SpecificationAttributeOption), x => result[CreateOptionKey("attr.option", x.LanguageId, x.EntityId)] = x.LocaleValue);

                return result;
            });

            return mappings;
        }

        #endregion

        #region Product Variants

        public async Task<int> GetVariantIdByAliasAsync(string variantAlias, int languageId = 0)
        {
            var result = 0;

            if (variantAlias.HasValue())
            {
                var mappings = await GetVariantIdByAliasMappingsAsync();

                if (!mappings.TryGetValue(CreateKey("vari", languageId, variantAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("vari", 0, variantAlias), out result);
                }
            }

            return result;
        }

        public async Task<int> GetVariantOptionIdByAliasAsync(string optionAlias, int variantId, int languageId = 0)
        {
            var result = 0;

            if (optionAlias.HasValue() && variantId != 0)
            {
                var mappings = await GetVariantIdByAliasMappingsAsync();

                if (!mappings.TryGetValue(CreateOptionKey("vari.option", languageId, variantId, optionAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("vari.option", 0, variantId, optionAlias), out result);
                }
            }

            return result;
        }

        public async Task<string> GetVariantAliasByIdAsync(int variantId, int languageId = 0)
        {
            string result = null;

            if (variantId != 0)
            {
                var mappings = await GetVariantAliasByIdMappingsAsync();

                if (!mappings.TryGetValue(CreateKey("vari", languageId, variantId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("vari", 0, variantId), out result);
                }
            }

            return result;
        }

        public async Task<string> GetVariantOptionAliasByIdAsync(int optionId, int languageId = 0)
        {
            string result = null;

            if (optionId != 0)
            {
                var mappings = await GetVariantAliasByIdMappingsAsync();

                if (!mappings.TryGetValue(CreateOptionKey("vari.option", languageId, optionId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("vari.option", 0, optionId), out result);
                }
            }

            return result;
        }

        public async Task ClearVariantCacheAsync()
        {
            await _cache.RemoveAsync(ALL_VARIANT_ID_BY_ALIAS_KEY);
            await _cache.RemoveAsync(ALL_VARIANT_ALIAS_BY_ID_KEY);
        }

        protected virtual async Task<IDictionary<string, int>> GetVariantIdByAliasMappingsAsync()
        {
            var mappings = await _cache.GetAsync(ALL_VARIANT_ID_BY_ALIAS_KEY, async () =>
            {
                var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                {
                    var variantPager = _db.ProductAttributes
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while ((await variantPager.ReadNextPageAsync<ProductAttribute>()).Out(out var variants))
                    {
                        foreach (var variant in variants)
                        {
                            result[CreateKey("vari", 0, variant.Alias)] = variant.Id;
                        }
                    }
                }

                {
                    var optionPager = _db.ProductVariantAttributeValues
                        .AsNoTracking()
                        .Include(x => x.ProductVariantAttribute)
                            .ThenInclude(x => x.ProductAttribute)
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while ((await optionPager.ReadNextPageAsync<ProductVariantAttributeValue>()).Out(out var options))
                    {
                        foreach (var option in options)
                        {
                            var variant = option.ProductVariantAttribute.ProductAttribute;
                            result[CreateOptionKey("vari.option", 0, variant.Id, option.Alias)] = option.Id;
                        }
                    }
                }

                var optionIdMappings = await _db.ProductVariantAttributeValues
                    .AsNoTracking()
                    .Include(x => x.ProductVariantAttribute)
                        .ThenInclude(x => x.ProductAttribute)
                    .Select(x => new
                    {
                        OptionId = x.Id,
                        VariantId = x.ProductVariantAttribute.ProductAttribute.Id
                    })
                    .ToListAsync();
                var optionIdMappingsDic = optionIdMappings.ToDictionary(x => x.OptionId, x => x.VariantId);

                await CacheLocalizedAliasAsync(nameof(ProductAttribute), x => result[CreateKey("vari", x.LanguageId, x.LocaleValue)] = x.EntityId);
                await CacheLocalizedAliasAsync(nameof(ProductVariantAttributeValue), x =>
                {
                    if (optionIdMappingsDic.TryGetValue(x.EntityId, out var variantId))
                    {
                        result[CreateOptionKey("vari.option", x.LanguageId, variantId, x.LocaleValue)] = x.EntityId;
                    }
                });

                return result;
            });

            return mappings;
        }

        protected virtual async Task<IDictionary<string, string>> GetVariantAliasByIdMappingsAsync()
        {
            var mappings = await _cache.GetAsync(ALL_VARIANT_ALIAS_BY_ID_KEY, async () =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                {
                    var variantPager = _db.ProductAttributes
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while ((await variantPager.ReadNextPageAsync<ProductAttribute>()).Out(out var variants))
                    {
                        foreach (var variant in variants)
                        {
                            result[CreateKey("vari", 0, variant.Id)] = variant.Alias;
                        }
                    }
                }

                {
                    var optionPager = _db.ProductVariantAttributeValues
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while ((await optionPager.ReadNextPageAsync<ProductVariantAttributeValue>()).Out(out var options))
                    {
                        foreach (var option in options)
                        {
                            result[CreateOptionKey("attr.option", 0, option.Id)] = option.Alias;
                        }
                    }
                }

                await CacheLocalizedAliasAsync(nameof(ProductAttribute), x => result[CreateKey("vari", x.LanguageId, x.EntityId)] = x.LocaleValue);
                await CacheLocalizedAliasAsync(nameof(ProductVariantAttributeValue), x => result[CreateOptionKey("vari.option", x.LanguageId, x.EntityId)] = x.LocaleValue);

                return result;
            });

            return mappings;
        }

        #endregion

        #region Common Facets

        public async Task<string> GetCommonFacetAliasByGroupKindAsync(FacetGroupKind kind, int languageId)
        {
            var mappings = await GetCommonFacetAliasByGroupKindMappingsAsync();

            return mappings.Get(FacetUtility.GetFacetAliasSettingKey(kind, languageId));
        }

        public async Task ClearCommonFacetCacheAsync()
        {
            await _cache.RemoveAsync(ALL_COMMONFACET_ALIAS_BY_KIND_KEY);
        }

        protected virtual async Task<IDictionary<string, string>> GetCommonFacetAliasByGroupKindMappingsAsync()
        {
            var mappings = await _cache.GetAsync(ALL_COMMONFACET_ALIAS_BY_KIND_KEY, async () =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var groupKinds = new FacetGroupKind[]
                {
                    FacetGroupKind.Category,
                    FacetGroupKind.Brand,
                    FacetGroupKind.Price,
                    FacetGroupKind.Rating,
                    FacetGroupKind.DeliveryTime,
                    FacetGroupKind.Availability,
                    FacetGroupKind.NewArrivals
                };

                var languageIds = await _db.Languages
                    .AsNoTracking()
                    .Where(x => x.Published)
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .ToListAsync();

                foreach (var languageId in languageIds)
                {
                    foreach (var groupKind in groupKinds)
                    {
                        var key = FacetUtility.GetFacetAliasSettingKey(groupKind, languageId);
                        var value = await _settingService.GetSettingByKeyAsync<string>(key);
                        if (value.HasValue())
                        {
                            result.Add(key, value);
                        }
                    }
                }

                return result;
            });

            return mappings;
        }

        #endregion

        #region Utilities

        protected static string CreateKey(string prefix, int languageId, string alias)
        {
            return $"{prefix}.{languageId}.{alias}";
        }
        protected static string CreateKey(string prefix, int languageId, int attributeId)
        {
            return $"{prefix}.{languageId}.{attributeId}";
        }

        protected static string CreateOptionKey(string prefix, int languageId, int attributeId, string optionAlias)
        {
            return $"{prefix}.{languageId}.{attributeId}.{optionAlias}";
        }
        protected static string CreateOptionKey(string prefix, int languageId, int optionId)
        {
            return $"{prefix}.{languageId}.{optionId}";
        }

        protected async Task CacheLocalizedAliasAsync(string localeKeyGroup, Action<LocalizedProperty> caching)
        {
            var properties = await _db.LocalizedProperties
                .AsNoTracking()
                .Where(x => x.LocaleKeyGroup == localeKeyGroup && x.LocaleKey == "Alias" && !string.IsNullOrWhiteSpace(x.LocaleValue))
                .ToListAsync();

            properties.ForEach(caching);
        }

        #endregion
    }
}
