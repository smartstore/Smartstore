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

        public ProductAttributeService(
            SmartDbContext db,
            ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        public virtual async Task<Multimap<string, int>> GetExportFieldMappingsAsync(string fieldPrefix)
        {
            Guard.NotEmpty(fieldPrefix, nameof(fieldPrefix));

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
            Guard.NotNull(productVariantAttribute, nameof(productVariantAttribute));
            Guard.NotZero(productVariantAttribute.Id, nameof(productVariantAttribute.Id));
            Guard.NotZero(productAttributeOptionsSetId, nameof(productAttributeOptionsSetId));

            var clearLocalizedEntityCache = false;
            var pvavName = nameof(ProductVariantAttributeValue);

            if (deleteExistingValues)
            {
                await _db.LoadCollectionAsync(productVariantAttribute, x => x.ProductVariantAttributeValues);

                var pvavIds = productVariantAttribute.ProductVariantAttributeValues
                    .Select(x => x.Id)
                    .ToArray();

                if (pvavIds.Any())
                {
                    _db.ProductVariantAttributeValues.RemoveRange(productVariantAttribute.ProductVariantAttributeValues);
                    await _db.SaveChangesAsync();

                    var oldLocalizedProperties = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(pvavName, pvavIds);
                    if (oldLocalizedProperties.Any())
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

            if (!optionsToCopy.Any())
            {
                return 0;
            }

            var existingValueNames = await _db.ProductVariantAttributeValues
                .Where(x => x.ProductVariantAttributeId == productVariantAttribute.Id)
                .Select(x => x.Name)
                .ToListAsync();

            var newValues = new Dictionary<int, ProductVariantAttributeValue>();

            foreach (var option in optionsToCopy)
            {
                if (!existingValueNames.Contains(option.Name))
                {
                    var pvav = option.Clone();
                    pvav.ProductVariantAttributeId = productVariantAttribute.Id;

                    newValues[option.Id] = pvav;
                }
            }

            if (!newValues.Any())
            {
                return 0;
            }

            // Save because we need the primary keys.
            await _db.ProductVariantAttributeValues.AddRangeAsync(newValues.Select(x => x.Value));
            var addedValues = await _db.SaveChangesAsync();

            var localizedProperties = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(ProductAttributeOption), newValues.Select(x => x.Key).ToArray());
            if (localizedProperties.Any())
            {
                clearLocalizedEntityCache = true;

                var localizedPropertiesMap = localizedProperties.ToMultimap(x => x.EntityId, x => x);

                foreach (var option in optionsToCopy)
                {
                    if (newValues.TryGetValue(option.Id, out var value) && localizedPropertiesMap.ContainsKey(option.Id))
                    {
                        foreach (var prop in localizedPropertiesMap[option.Id])
                        {
                            _db.LocalizedProperties.Add(new LocalizedProperty
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

            return addedValues;
        }

        public virtual Task<ICollection<int>> GetAttributeCombinationFileIdsAsync(Product product)
        {
            Guard.NotNull(product, nameof(product));

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
            if (!source.Any())
            {
                return new HashSet<int>();
            }

            var result = source
                .SelectMany(x => x.SplitSafe(','))
                .Select(x => x.ToInt())
                .Where(x => x != 0);

            return new HashSet<int>(result);
        }

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

            if (!attributes.Any())
            {
                return 0;
            }

            var mappedAttributes = attributes
                .SelectMany(x => x.ProductVariantAttributeValues)
                .ToDictionarySafe(x => x.Id, x => x.ProductVariantAttribute);

            var toCombine = new List<List<ProductVariantAttributeValue>>();
            var resultMatrix = new List<List<ProductVariantAttributeValue>>();
            var tmpValues = new List<ProductVariantAttributeValue>();

            attributes
                .Where(x => x.ProductVariantAttributeValues.Any())
                .Each(x => toCombine.Add(x.ProductVariantAttributeValues.ToList()));

            if (!toCombine.Any())
            {
                return 0;
            }

            CombineAll(0, tmpValues);

            var addedCombinations = 0;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important))
            {
                foreach (var values in resultMatrix)
                {
                    var attributeSelection = new ProductVariantAttributeSelection(string.Empty);

                    foreach (var value in values)
                    {
                        attributeSelection.AddAttributeValue(mappedAttributes[value.Id].Id, value.Id);
                    }

                    var combination = new ProductVariantAttributeCombination
                    {
                        ProductId = productId,
                        RawAttributes = attributeSelection.AsJson(),
                        StockQuantity = 10000,
                        AllowOutOfStockOrders = true,
                        IsActive = true
                    };

                    _db.ProductVariantAttributeCombinations.Add(combination);
                }

                addedCombinations = await scope.CommitAsync();
            }

            //foreach (var y in resultMatrix)
            //{
            //	var sb = new System.Text.StringBuilder();
            //	foreach (var x in y)
            //	{
            //		sb.AppendFormat("{0} ", x.Name);
            //	}
            //	sb.ToString().Dump();
            //}

            return addedCombinations;

            void CombineAll(int row, List<ProductVariantAttributeValue> tmp)
            {
                var combine = toCombine[row];

                for (var col = 0; col < combine.Count; ++col)
                {
                    var lst = new List<ProductVariantAttributeValue>(tmp)
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
        }
    }
}
