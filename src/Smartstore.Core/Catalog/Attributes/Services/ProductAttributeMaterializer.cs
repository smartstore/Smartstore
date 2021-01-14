using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Attributes
{
    public partial class ProductAttributeMaterializer : IProductAttributeMaterializer
    {
        private static readonly TimeSpan ProductAttributesCacheDuration = TimeSpan.FromSeconds(30);

        // 0 = ProductId
        internal const string UNAVAILABLE_COMBINATIONS_KEY = "attributecombination:unavailable-{0}";
        internal const string UNAVAILABLE_COMBINATIONS_PATTERN_KEY = "attributecombination:unavailable-*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly PerformanceSettings _performanceSettings;

        public ProductAttributeMaterializer(
            SmartDbContext db,
            ICacheManager cache,
            PerformanceSettings performanceSettings)
        {
            _db = db;
            _cache = cache;
            _performanceSettings = performanceSettings;
        }

        public virtual async Task<IList<ProductVariantAttribute>> MaterializeProductVariantAttributesAsync(ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var pvaIds = selection.AttributesMap
                .Select(x => x.Key)
                .ToArray();

            if (pvaIds.Any())
            {
                var query = _db.ProductVariantAttributes
                    .Include(x => x.ProductAttribute)
                    .AsNoTracking()
                    .AsCaching(ProductAttributesCacheDuration)
                    .Where(x => pvaIds.Contains(x.Id));

                var attributes = await query.ToListAsync();
                return attributes;
            }

            return new List<ProductVariantAttribute>();
        }

        public virtual async Task<IList<ProductVariantAttributeValue>> MaterializeProductVariantAttributeValuesAsync(ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var valueIds = selection.GetAttributeValueIds();
            if (valueIds.Any())
            {
                var query = _db.ProductVariantAttributeValues
                    .Include(x => x.ProductVariantAttribute)
                        .ThenInclude(x => x.ProductAttribute)
                    .AsNoTracking()
                    .AsCaching(ProductAttributesCacheDuration)
                    .ApplyValueFilter(valueIds);

                var values = await query.ToListAsync();
                return values;
            }

            // That's what the old ported code did:
            //if (selection?.AttributesMap?.Any() ?? false)
            //{
            //    var pvaIds = selection.AttributesMap.Select(x => x.Key).ToArray();
            //    if (pvaIds.Any())
            //    {
            //        var attributes = await _db.ProductVariantAttributes
            //            .AsNoTracking()
            //            .AsCaching(ProductAttributesCacheDuration)
            //            .Where(x => pvaIds.Contains(x.Id))
            //            .OrderBy(x => x.DisplayOrder)
            //            .ToListAsync();

            //        var valueIds = GetAttributeValueIds(attributes, selection).ToArray();

            //        var values = await _db.ProductVariantAttributeValues
            //            .AsNoTracking()
            //            .AsCaching(ProductAttributesCacheDuration)
            //            .ApplyValueFilter(valueIds)
            //            .ToListAsync();

            //        return values;
            //    }
            //}

            return new List<ProductVariantAttributeValue>();
        }

        // Depending on caller it will probably become obsolete:
        public virtual IList<ProductVariantAttributeValue> MaterializeProductVariantAttributeValues(ProductVariantAttributeSelection selection, IEnumerable<ProductVariantAttribute> attributes)
        {
            var result = new List<ProductVariantAttributeValue>();

            if (selection?.AttributesMap?.Any() ?? false)
            {
                var listTypeAttributeIds = attributes
                    .Where(x => x.IsListTypeAttribute())
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .Distinct()
                    .ToArray();

                var valueIds = selection.AttributesMap
                    .Where(x => listTypeAttributeIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .Select(x => x.ToString())
                    .Where(x => x.HasValue())   // Avoid exception when string is empty.
                    .Select(x => x.ToInt())
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                foreach (int valueId in valueIds)
                {
                    foreach (var attribute in attributes)
                    {
                        var attributeValue = attribute.ProductVariantAttributeValues.FirstOrDefault(x => x.Id == valueId);
                        if (attributeValue != null)
                        {
                            result.Add(attributeValue);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public virtual async Task<ProductVariantAttributeCombination> FindAttributeCombinationAsync(int productId, ProductVariantAttributeSelection selection)
        {
            if (productId == 0 || !(selection?.AttributesMap?.Any() ?? false))
            {
                return null;
            }

            var combinations = await _db.ProductVariantAttributeCombinations
                .AsNoTracking()
                .AsCaching(ProductAttributesCacheDuration)
                .Where(x => x.ProductId == productId)
                .Select(x => new { x.Id, x.AttributesXml })
                .ToListAsync();

            foreach (var combination in combinations)
            {
                if (selection.Equals(new ProductVariantAttributeSelection(combination.AttributesXml)))
                {
                    return await _db.ProductVariantAttributeCombinations.FindByIdAsync(combination.Id);
                }
            }

            return null;
        }

        public virtual async Task<CombinationAvailabilityInfo> IsCombinationAvailableAsync(
            Product product,
            IEnumerable<ProductVariantAttribute> attributes,
            IEnumerable<ProductVariantAttributeValue> selectedValues,
            ProductVariantAttributeValue currentValue)
        {
            if (product == null ||
                _performanceSettings.MaxUnavailableAttributeCombinations <= 0 ||
                !(selectedValues?.Any() ?? false))
            {
                return null;
            }

            // Get unavailable combinations.
            var unavailableCombinations = await _cache.GetAsync(UNAVAILABLE_COMBINATIONS_KEY.FormatInvariant(product.Id), async o =>
            {
                o.ExpiresIn(TimeSpan.FromMinutes(10));

                var data = new Dictionary<string, CombinationAvailabilityInfo>();
                var query = _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => x.ProductId == product.Id);

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
                {
                    query = query.Where(x => !x.IsActive || (x.StockQuantity <= 0 && !x.AllowOutOfStockOrders));
                }
                else
                {
                    query = query.Where(x => !x.IsActive);
                }

                // Do not proceed if there are too many unavailable combinations.
                var unavailableCombinationsCount = await query.CountAsync();

                if (unavailableCombinationsCount <= _performanceSettings.MaxUnavailableAttributeCombinations)
                {
                    var pager = query.ToFastPager();

                    while ((await pager.ReadNextPageAsync<ProductVariantAttributeCombination>()).Out(out var combinations))
                    {
                        foreach (var combination in combinations)
                        {
                            var selection = new ProductVariantAttributeSelection(combination.AttributesXml);
                            if (selection.AttributesMap.Any())
                            {
                                // <ProductVariantAttribute.Id>:<ProductVariantAttributeValue.Id>[,...]
                                var valuesKeys = selection.AttributesMap
                                    .OrderBy(x => x.Key)
                                    .Select(x => $"{x.Key}:{string.Join(",", x.Value.OrderBy(y => y))}");

                                data[string.Join("-", valuesKeys)] = new CombinationAvailabilityInfo
                                {
                                    IsActive = combination.IsActive,
                                    IsOutOfStock = combination.StockQuantity <= 0 && !combination.AllowOutOfStockOrders
                                };
                            }
                        }
                    }
                }

                return data;
            });

            if (!unavailableCombinations.Any())
            {
                return null;
            }

            using var pool = StringBuilderPool.Instance.Get(out var builder);
            var selectedValuesMap = selectedValues.ToMultimap(x => x.ProductVariantAttributeId, x => x);

            if (attributes == null || currentValue == null)
            {
                // Create key to test selectedValues.
                foreach (var kvp in selectedValuesMap.OrderBy(x => x.Key))
                {
                    Append(builder, kvp.Key, kvp.Value.Select(x => x.Id).Distinct());
                }
            }
            else
            {
                // Create key to test currentValue.
                foreach (var attribute in attributes.OrderBy(x => x.Id))
                {
                    IEnumerable<int> valueIds;

                    var selectedIds = selectedValuesMap.ContainsKey(attribute.Id)
                        ? selectedValuesMap[attribute.Id].Select(x => x.Id)
                        : null;

                    if (attribute.Id == currentValue.ProductVariantAttributeId)
                    {
                        // Attribute to be tested.
                        if (selectedIds != null && attribute.IsMultipleChoice)
                        {
                            // Take selected values and append current value.
                            valueIds = selectedIds.Append(currentValue.Id).Distinct();
                        }
                        else
                        {
                            // Single selection attribute -> take current value.
                            valueIds = new[] { currentValue.Id };
                        }
                    }
                    else
                    {
                        // Other attribute.
                        if (selectedIds != null)
                        {
                            // Take selected value(s).
                            valueIds = selectedIds;
                        }
                        else
                        {
                            // No selected value -> no unavailable combination.
                            return null;
                        }
                    }

                    Append(builder, attribute.Id, valueIds);
                }
            }

            var key = builder.ToString();
            //$"{!unavailableCombinations.ContainsKey(key),-5} {currentValue.ProductVariantAttributeId}:{currentValue.Id} -> {key}".Dump();

            if (unavailableCombinations.TryGetValue(key, out var availability))
            {
                return availability;
            }

            return null;

            static void Append(StringBuilder sb, int pvaId, IEnumerable<int> pvavIds)
            {
                var idsStr = string.Join(",", pvavIds.OrderBy(x => x));

                if (sb.Length > 0)
                {
                    sb.Append('-');
                }
                sb.Append($"{pvaId}:{idsStr}");
            }
        }
    }
}
