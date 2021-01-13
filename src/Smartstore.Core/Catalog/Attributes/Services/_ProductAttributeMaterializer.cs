using System;
using System.Collections.Generic;
using System.Linq;
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


        public virtual ICollection<ProductVariantAttributeValue> MaterializeProductVariantAttributeValues(ProductVariantAttributeSelection attributeSelection, IEnumerable<ProductVariantAttribute> attributes)
        {
            var result = new HashSet<ProductVariantAttributeValue>();

            if (!(attributeSelection?.AttributesMap?.Any() ?? false))
            {
                return result;
            }

            var allValueIds = new HashSet<int>();
            var relevantAttributes = attributes
                .Where(x => x.ShouldHaveValues())
                .OrderBy(x => x.DisplayOrder)
                .ToArray();

            foreach (var pva in relevantAttributes)
            {
                var pvaValues = attributeSelection.AttributesMap
                    .Where(x => x.Key == pva.Id)
                    .Select(x => x.Value);

                var ids = pvaValues
                    .Select(x => x.ToString())
                    .Where(x => x.HasValue())
                    .Select(x => x.ToInt())
                    .ToArray();

                allValueIds.UnionWith(ids);
            }

            foreach (int id in allValueIds)
            {
                foreach (var attribute in attributes)
                {
                    var attributeValue = attribute.ProductVariantAttributeValues.FirstOrDefault(x => x.Id == id);
                    if (attributeValue != null)
                    {
                        result.Add(attributeValue);
                        break;
                    }
                }
            }

            return result;
        }

        public virtual bool AreProductAttributesEqual(AttributeSelection attributes1, AttributeSelection attributes2)
        {
            Guard.NotNull(attributes1, nameof(attributes1));
            Guard.NotNull(attributes2, nameof(attributes2));

            // TODO: (mg) (core) Implement this in AttributeSelection.IEquatable<AttributeSelection>.Equals() and remove AreProductAttributesEqual later

            var map1 = attributes1.AttributesMap;
            var map2 = attributes2.AttributesMap;

            if (map1.Count() != map2.Count())
            {
                return false;
            }

            foreach (var kvp in map1)
            {
                if (!map2.Any(x => x.Key == kvp.Key))
                {
                    // The second list does not contain this key > not equal.
                    return false;
                }

                // Compare the values.
                var values1 = kvp.Value;
                var values2 = map2
                    .Where(x => x.Key == kvp.Key)
                    .Select(x => x.Value);

                if (values1.Count != values2.Count())
                {
                    // Number of values differ > not equal.
                    return false;
                }

                foreach (var value1 in values1)
                {
                    var str1 = value1.ToString().TrimSafe();

                    if (!values2.Any(x => x.ToString().TrimSafe().EqualsNoCase(str1)))
                    {
                        // The second values list for this attribute does not contain this value > not equal.
                        return false;
                    }
                }
            }

            return true;
        }

        public virtual async Task<ProductVariantAttributeCombination> FindAttributeCombinationAsync(int productId, ProductVariantAttributeSelection attributes)
        {
            if (productId == 0 || !(attributes?.AttributesMap?.Any() ?? false))
            {
                return null;
            }

            var combinations = await _db.ProductVariantAttributeCombinations
                .AsCaching(TimeSpan.FromSeconds(30))
                .Where(x => x.ProductId == productId)
                .Select(x => new { x.Id, x.AttributesXml })
                .ToListAsync();

            foreach (var combination in combinations)
            {
                if (AreProductAttributesEqual(new ProductVariantAttributeSelection(combination.AttributesXml), attributes))
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
