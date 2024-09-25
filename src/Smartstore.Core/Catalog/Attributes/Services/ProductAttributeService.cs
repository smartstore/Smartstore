using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    public partial class ProductAttributeService : IProductAttributeService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public ProductAttributeService(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        public virtual async Task<Multimap<string, int>> GetExportFieldMappingsAsync(string fieldPrefix)
        {
            Guard.NotEmpty(fieldPrefix);

            var result = new Multimap<string, int>(StringComparer.OrdinalIgnoreCase);

            if (!fieldPrefix.EndsWith(':'))
            {
                fieldPrefix += ":";
            }

            var mappings = await _db.ProductAttributes
                .Where(x => !string.IsNullOrEmpty(x.ExportMappings))
                .Select(x => new
                {
                    x.Id,
                    x.ExportMappings
                })
                .ToListAsync();

            foreach (var mapping in mappings)
            {
                var rows = mapping.ExportMappings.SplitSafe(Environment.NewLine)
                    .Where(x => x.StartsWith(fieldPrefix, StringComparison.InvariantCultureIgnoreCase));

                foreach (var row in rows)
                {
                    var exportFieldName = row[fieldPrefix.Length..].TrimEnd();
                    if (exportFieldName.HasValue())
                    {
                        result.Add(exportFieldName, mapping.Id);
                    }
                }
            }

            return result;
        }

        public virtual async Task<int> CopyAttributeOptionsAsync(
            ProductVariantAttribute productVariantAttribute,
            int productAttributeOptionsSetId,
            bool deleteExistingValues)
        {
            Guard.NotNull(productVariantAttribute);
            Guard.NotZero(productVariantAttribute.Id);
            Guard.NotZero(productAttributeOptionsSetId);

            var clearLocalizedEntityCache = false;
            var pvavName = nameof(ProductVariantAttributeValue);

            if (deleteExistingValues)
            {
                await _db.LoadCollectionAsync(productVariantAttribute, x => x.ProductVariantAttributeValues);

                var valueIds = productVariantAttribute.ProductVariantAttributeValues.Select(x => x.Id).ToArray();
                if (valueIds.Length > 0)
                {
                    _db.ProductVariantAttributeValues.RemoveRange(productVariantAttribute.ProductVariantAttributeValues);
                    await _db.SaveChangesAsync();

                    var oldLocalizedProperties = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(pvavName, valueIds);
                    if (oldLocalizedProperties.Count > 0)
                    {
                        clearLocalizedEntityCache = true;
                        _db.LocalizedProperties.RemoveRange(oldLocalizedProperties);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            var optionsToCopy = await _db.ProductAttributeOptions
                .AsNoTracking()
                .Where(x => x.ProductAttributeOptionsSetId == productAttributeOptionsSetId)
                .ToListAsync();

            if (optionsToCopy.Count == 0)
            {
                return 0;
            }

            var newValues = new Dictionary<int, ProductVariantAttributeValue>();
            var existingValueNames = await _db.ProductVariantAttributeValues
                .Where(x => x.ProductVariantAttributeId == productVariantAttribute.Id)
                .Select(x => x.Name)
                .ToListAsync();

            foreach (var option in optionsToCopy)
            {
                if (!existingValueNames.Contains(option.Name))
                {
                    var pvav = option.Clone();
                    pvav.ProductVariantAttributeId = productVariantAttribute.Id;
                    newValues[option.Id] = pvav;
                }
            }

            if (newValues.Count == 0)
            {
                return 0;
            }

            // Save because we need the primary keys.
            await _db.ProductVariantAttributeValues.AddRangeAsync(newValues.Select(x => x.Value));
            await _db.SaveChangesAsync();

            var localizations = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(ProductAttributeOption), newValues.Select(x => x.Key).ToArray());
            if (localizations.Count > 0)
            {
                clearLocalizedEntityCache = true;
                var localizationsMap = localizations.ToMultimap(x => x.EntityId, x => x);

                foreach (var option in optionsToCopy)
                {
                    if (newValues.TryGetValue(option.Id, out var value) && localizationsMap.TryGetValues(option.Id, out var props))
                    {
                        foreach (var prop in props)
                        {
                            _db.LocalizedProperties.Add(new()
                            {
                                EntityId = value.Id,
                                LocaleKeyGroup = pvavName,
                                LocaleKey = prop.LocaleKey,
                                LocaleValue = prop.LocaleValue,
                                LanguageId = prop.LanguageId,
                                IsHidden = prop.IsHidden,
                                CreatedOnUtc = DateTime.UtcNow,
                                UpdatedOnUtc = prop.UpdatedOnUtc,
                                CreatedBy = prop.CreatedBy,
                                UpdatedBy = prop.UpdatedBy
                            });
                        }
                    }
                }

                await _db.SaveChangesAsync();
            }

            if (clearLocalizedEntityCache)
            {
                await _localizedEntityService.ClearCacheAsync();
            }

            return newValues.Count;
        }

        public virtual Task<ICollection<int>> GetAttributeCombinationFileIdsAsync(Product product)
        {
            Guard.NotNull(product);

            if (!_db.IsCollectionLoaded(product, x => x.ProductVariantAttributeCombinations))
            {
                return GetAttributeCombinationFileIdsAsync(product.Id);
            }

            var fileIds = product.ProductVariantAttributeCombinations
                .Where(x => !string.IsNullOrEmpty(x.AssignedMediaFileIds) && x.IsActive)
                .Select(x => x.AssignedMediaFileIds)
                .ToList();

            return Task.FromResult(CreateFileIdSet(fileIds));
        }

        public virtual async Task<ICollection<int>> GetAttributeCombinationFileIdsAsync(int productId)
        {
            if (productId == 0)
            {
                return new HashSet<int>();
            }

            var fileIds = await _db.ProductVariantAttributeCombinations
                .Where(x => x.ProductId == productId && !string.IsNullOrEmpty(x.AssignedMediaFileIds) && x.IsActive)
                .Select(x => x.AssignedMediaFileIds)
                .ToListAsync();

            return CreateFileIdSet(fileIds);
        }

        private static ICollection<int> CreateFileIdSet(List<string> source)
        {
            if (source.Count == 0)
            {
                return [];
            }

            var result = source
                .SelectMany(x => x.SplitSafe(','))
                .Select(x => x.ToInt())
                .Where(x => x != 0);

            return new HashSet<int>(result);
        }

        #region Creating all attribute combinations

        public virtual async Task<int> CreateAllAttributeCombinationsAsync(int productId)
        {
            if (productId == 0)
            {
                return 0;
            }

            // Delete all existing combinations for this product.
            await _db.ProductVariantAttributeCombinations
                .Where(x => x.ProductId == productId)
                .ExecuteDeleteAsync();

            var attributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductVariantAttributeValues)
                .Where(x => x.ProductId == productId)
                .ApplyListTypeFilter()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            if (attributes.Count == 0)
            {
                return 0;
            }

            var attributeValuesMap = attributes
                .SelectMany(x => x.ProductVariantAttributeValues)
                .ToDictionarySafe(x => x.Id);

            var numAdded = 0;
            var toCombine = new List<List<CombinationItem>>();
            var resultMatrix = new List<List<CombinationItem>>();
            var tmpItems = new List<CombinationItem>();

            // 1. Build a matrix "toCombine" with all attribute values to be combined.
            foreach (var pva in attributes.Where(x => x.ProductVariantAttributeValues.Count > 0))
            {
                var values = pva.ProductVariantAttributeValues;

                if (pva.IsMultipleChoice && values.Count > 1)
                {
                    var combinations = GetCombinations(values.Select(v => v.Id).ToList());

                    toCombine.Add(combinations
                        .Select(x => new CombinationItem(pva.Id, [.. x]))
                        .ToList());
                }
                else
                {
                    toCombine.Add(values
                        .Select(v => new CombinationItem(pva.Id, v.Id))
                        .ToList());
                }
            }

            if (toCombine.Count == 0)
            {
                return 0;
            }

            // 2. Combine all items of "toCombine" and put the combinations to "resultMatrix".
            CombineAll(0, tmpItems);

            // 3. Create ProductVariantAttributeCombination entities from "resultMatrix" and store them in the database.
            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important))
            {
                foreach (var items in resultMatrix)
                {
                    var selection = new ProductVariantAttributeSelection(string.Empty);

                    foreach (var item in items)
                    {
                        selection.AddAttribute(item.AttributeId, item.ValueIds.Select(id => (object)id));
                    }

                    _db.ProductVariantAttributeCombinations.Add(new()
                    {
                        ProductId = productId,
                        RawAttributes = selection.AsJson(),
                        StockQuantity = 10000,
                        AllowOutOfStockOrders = true,
                        IsActive = true
                    });

                    if ((++numAdded % 100) == 0)
                    {
                        await scope.CommitAsync();
                    }
                }

                await scope.CommitAsync();
            }

            return numAdded;

            void CombineAll(int row, List<CombinationItem> tmp)
            {
                var combine = toCombine[row];

                for (var col = 0; col < combine.Count; ++col)
                {
                    var lst = new List<CombinationItem>(tmp)
                    {
                        combine[col]
                    };

                    if (row == (toCombine.Count - 1))
                    {
                        resultMatrix.Add(lst);
                    }
                    else
                    {
                        CombineAll(row + 1, lst);
                    }
                }
            }

            //void Dump(List<List<CombinationItem>> list)
            //{
            //    var sb = new System.Text.StringBuilder();
            //    foreach (var y in list)
            //    {
            //        foreach (var x in y)
            //        {
            //            if (y.IndexOf(x) != 0)
            //            {
            //                sb.Append(", ");
            //            }
            //            sb.Append(string.Join('+', x.ValueIds.Select(id => attributeValueMap[id]?.Name)));
            //        }
            //        sb.AppendLine();
            //    }
            //    sb.ToString().Dump();
            //}
        }

        static List<List<T>> GetCombinations<T>(List<T> list)
        {
            var result = new List<List<T>>
            {
                new()
            };

            foreach (var item in list)
            {
                var count = result.Count;
                for (var i = 0; i < count; i++)
                {
                    var newCombination = new List<T>(result[i]) { item };
                    result.Add(newCombination);
                }
            }

            result.RemoveAt(0);
            return result;
        }

        record CombinationItem
        {
            public CombinationItem(int attributeId, params int[] valueIds)
            {
                AttributeId = attributeId;
                ValueIds.AddRange(valueIds);
            }

            public int AttributeId { get; }
            public List<int> ValueIds { get; } = [];
        }

        #endregion
    }
}
