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

        public int GetAttributeIdByAlias(string attributeAlias, int languageId = 0)
        {
            var result = 0;

            if (attributeAlias.HasValue())
            {
                var mappings = GetAttributeIdByAliasMappings();

                if (!mappings.TryGetValue(CreateKey("attr", languageId, attributeAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("attr", 0, attributeAlias), out result);
                }
            }

            return result;
        }

        public int GetAttributeOptionIdByAlias(string optionAlias, int attributeId, int languageId = 0)
        {
            var result = 0;

            if (optionAlias.HasValue() && attributeId != 0)
            {
                var mappings = GetAttributeIdByAliasMappings();

                if (!mappings.TryGetValue(CreateOptionKey("attr.option", languageId, attributeId, optionAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("attr.option", 0, attributeId, optionAlias), out result);
                }
            }

            return result;
        }

        public string GetAttributeAliasById(int attributeId, int languageId = 0)
        {
            string result = null;

            if (attributeId != 0)
            {
                var mappings = GetAttributeAliasByIdMappings();

                if (!mappings.TryGetValue(CreateKey("attr", languageId, attributeId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("attr", 0, attributeId), out result);
                }
            }

            return result;
        }

        public string GetAttributeOptionAliasById(int optionId, int languageId = 0)
        {
            string result = null;

            if (optionId != 0)
            {
                var mappings = GetAttributeAliasByIdMappings();

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

        protected virtual IDictionary<string, int> GetAttributeIdByAliasMappings()
        {
            var mappings = _cache.Get(ALL_ATTRIBUTE_ID_BY_ALIAS_KEY, () =>
            {
                var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var optionIdMappings = new Dictionary<int, int>();

                var pager = _db.SpecificationAttributes
                    .AsNoTracking()
                    .Include(x => x.SpecificationAttributeOptions)
                    .OrderBy(x => x.Id)
                    .ToFastPager(500);

                while (pager.ReadNextPage(out var attributes))
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

                CacheLocalizedAlias(nameof(SpecificationAttribute), x => result[CreateKey("attr", x.LanguageId, x.LocaleValue)] = x.EntityId);
                CacheLocalizedAlias(nameof(SpecificationAttributeOption), x =>
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

        protected virtual IDictionary<string, string> GetAttributeAliasByIdMappings()
        {
            var mappings = _cache.Get(ALL_ATTRIBUTE_ALIAS_BY_ID_KEY, () =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var pager = _db.SpecificationAttributes
                    .AsNoTracking()
                    .Include(x => x.SpecificationAttributeOptions)
                    .OrderBy(x => x.Id)
                    .ToFastPager(500);

                while (pager.ReadNextPage(out var attributes))
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

                CacheLocalizedAlias(nameof(SpecificationAttribute), x => result[CreateKey("attr", x.LanguageId, x.EntityId)] = x.LocaleValue);
                CacheLocalizedAlias(nameof(SpecificationAttributeOption), x => result[CreateOptionKey("attr.option", x.LanguageId, x.EntityId)] = x.LocaleValue);

                return result;
            });

            return mappings;
        }

        #endregion

        #region Product Variants

        public int GetVariantIdByAlias(string variantAlias, int languageId = 0)
        {
            var result = 0;

            if (variantAlias.HasValue())
            {
                var mappings = GetVariantIdByAliasMappings();

                if (!mappings.TryGetValue(CreateKey("vari", languageId, variantAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("vari", 0, variantAlias), out result);
                }
            }

            return result;
        }

        public int GetVariantOptionIdByAlias(string optionAlias, int variantId, int languageId = 0)
        {
            var result = 0;

            if (optionAlias.HasValue() && variantId != 0)
            {
                var mappings = GetVariantIdByAliasMappings();

                if (!mappings.TryGetValue(CreateOptionKey("vari.option", languageId, variantId, optionAlias), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateOptionKey("vari.option", 0, variantId, optionAlias), out result);
                }
            }

            return result;
        }

        public string GetVariantAliasById(int variantId, int languageId = 0)
        {
            string result = null;

            if (variantId != 0)
            {
                var mappings = GetVariantAliasByIdMappings();

                if (!mappings.TryGetValue(CreateKey("vari", languageId, variantId), out result) && languageId != 0)
                {
                    mappings.TryGetValue(CreateKey("vari", 0, variantId), out result);
                }
            }

            return result;
        }

        public string GetVariantOptionAliasById(int optionId, int languageId = 0)
        {
            string result = null;

            if (optionId != 0)
            {
                var mappings = GetVariantAliasByIdMappings();

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

        protected virtual IDictionary<string, int> GetVariantIdByAliasMappings()
        {
            var mappings = _cache.Get(ALL_VARIANT_ID_BY_ALIAS_KEY, () =>
            {
                var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                {
                    var variantPager = _db.ProductAttributes
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while (variantPager.ReadNextPage(out var variants))
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

                    while (optionPager.ReadNextPage(out var options))
                    {
                        foreach (var option in options)
                        {
                            var variant = option.ProductVariantAttribute.ProductAttribute;
                            result[CreateOptionKey("vari.option", 0, variant.Id, option.Alias)] = option.Id;
                        }
                    }
                }

                var optionIdMappings = _db.ProductVariantAttributeValues
                    .AsNoTracking()
                    .Include(x => x.ProductVariantAttribute)
                        .ThenInclude(x => x.ProductAttribute)
                    .Select(x => new
                    {
                        OptionId = x.Id,
                        VariantId = x.ProductVariantAttribute.ProductAttribute.Id
                    })
                    .ToList();
                var optionIdMappingsDic = optionIdMappings.ToDictionary(x => x.OptionId, x => x.VariantId);

                CacheLocalizedAlias(nameof(ProductAttribute), x => result[CreateKey("vari", x.LanguageId, x.LocaleValue)] = x.EntityId);
                CacheLocalizedAlias(nameof(ProductVariantAttributeValue), x =>
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

        protected virtual IDictionary<string, string> GetVariantAliasByIdMappings()
        {
            var mappings = _cache.Get(ALL_VARIANT_ALIAS_BY_ID_KEY, () =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                {
                    var variantPager = _db.ProductAttributes
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Alias))
                        .OrderBy(x => x.Id)
                        .ToFastPager(500);

                    while (variantPager.ReadNextPage(out var variants))
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

                    while (optionPager.ReadNextPage(out var options))
                    {
                        foreach (var option in options)
                        {
                            result[CreateOptionKey("attr.option", 0, option.Id)] = option.Alias;
                        }
                    }
                }

                CacheLocalizedAlias(nameof(ProductAttribute), x => result[CreateKey("vari", x.LanguageId, x.EntityId)] = x.LocaleValue);
                CacheLocalizedAlias(nameof(ProductVariantAttributeValue), x => result[CreateOptionKey("vari.option", x.LanguageId, x.EntityId)] = x.LocaleValue);

                return result;
            });

            return mappings;
        }

        #endregion

        #region Common Facets

        public string GetCommonFacetAliasByGroupKind(FacetGroupKind kind, int languageId)
        {
            var mappings = GetCommonFacetAliasByGroupKindMappings();
            return mappings.Get(FacetUtility.GetFacetAliasSettingKey(kind, languageId));
        }

        public Task ClearCommonFacetCacheAsync()
        {
            return _cache.RemoveAsync(ALL_COMMONFACET_ALIAS_BY_KIND_KEY);
        }

        protected virtual IDictionary<string, string> GetCommonFacetAliasByGroupKindMappings()
        {
            var mappings = _cache.Get(ALL_COMMONFACET_ALIAS_BY_KIND_KEY, o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

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

                var languageIds = _db.Languages
                    .AsNoTracking()
                    .Where(x => x.Published)
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .ToList();

                foreach (var languageId in languageIds)
                {
                    foreach (var groupKind in groupKinds)
                    {
                        var key = FacetUtility.GetFacetAliasSettingKey(groupKind, languageId);
                        var value = _settingService.GetSettingByKey<string>(key);
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

        protected void CacheLocalizedAlias(string localeKeyGroup, Action<LocalizedProperty> caching)
        {
            var properties = _db.LocalizedProperties
                .AsNoTracking()
                .Where(x => x.LocaleKeyGroup == localeKeyGroup && x.LocaleKey == "Alias" && !string.IsNullOrWhiteSpace(x.LocaleValue))
                .ToList();

            properties.ForEach(caching);
        }

        #endregion
    }
}
