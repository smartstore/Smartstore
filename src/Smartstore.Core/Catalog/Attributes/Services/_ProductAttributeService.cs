using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Batching;

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

            if (!fieldPrefix.EndsWith(":"))
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

        public virtual async Task<IList<ProductVariantAttribute>> GetProductVariantAttributesByIdsAsync(
            IEnumerable<int> productVariantAttributeIds,
            IEnumerable<ProductVariantAttribute> attributes = null)
        {
            if (productVariantAttributeIds?.Any() ?? false)
            {
                if (attributes != null)
                {
                    var ids = new List<int>();
                    var result = new List<ProductVariantAttribute>();

                    foreach (var id in productVariantAttributeIds)
                    {
                        var pva = attributes.FirstOrDefault(x => x.Id == id);
                        if (pva == null)
                        {
                            ids.Add(id);
                        }
                        else
                        {
                            result.Add(pva);
                        }
                    }

                    if (ids.Any())
                    {
                        var newLoadedMappings = await GetSwitchedLoadedAttributeMappings(ids);
                        result.AddRange(newLoadedMappings);
                    }

                    // Sort by passed identifier sequence.
                    return result.OrderBySequence(productVariantAttributeIds).ToList();
                }

                return await GetSwitchedLoadedAttributeMappings(productVariantAttributeIds);
            }

            return new List<ProductVariantAttribute>();

            async Task<IList<ProductVariantAttribute>> GetSwitchedLoadedAttributeMappings(IEnumerable<int> pvaIds)
            {
                var count = pvaIds?.Count() ?? 0;

                if (count > 0)
                {
                    if (count == 1)
                    {
                        var pva = await _db.ProductVariantAttributes.FindByIdAsync(pvaIds.First());
                        if (pva != null)
                        {
                            return new List<ProductVariantAttribute> { pva };
                        }
                    }
                    else
                    {
                        var result = new List<ProductVariantAttribute>();

                        foreach (var idsChunk in pvaIds.Slice(500))
                        {
                            var chunk = await _db.ProductVariantAttributes
                                .Where(x => idsChunk.Contains(x.Id))
                                .ToListAsync();

                            result.AddRange(chunk);
                        }

                        return result
                            .OrderBy(x => x.DisplayOrder)
                            .ToList();
                    }
                }

                return new List<ProductVariantAttribute>();
            }
        }

        public virtual async Task<int> CopyAttributeOptionsAsync(ProductVariantAttribute productVariantAttribute, int productAttributeOptionsSetId, bool deleteExistingValues)
        {
            Guard.NotNull(productVariantAttribute, nameof(productVariantAttribute));
            Guard.NotZero(productVariantAttribute.Id, nameof(productVariantAttribute.Id));
            Guard.NotZero(productAttributeOptionsSetId, nameof(productAttributeOptionsSetId));

            if (deleteExistingValues)
            {
                await _db.LoadCollectionAsync(productVariantAttribute, x => x.ProductVariantAttributeValues);
                await _db.ProductVariantAttributeValues.BatchDeleteAsync();
            }

            var attributeOptions = await _db.ProductAttributeOptions
                .AsNoTracking()
                .Where(x => x.ProductAttributeOptionsSetId == productAttributeOptionsSetId)
                .ToListAsync();

            if (!attributeOptions.Any())
            {
                return 0;
            }

            var existingValueNames = await _db.ProductVariantAttributeValues
                .Where(x => x.ProductVariantAttributeId == productVariantAttribute.Id)
                .Select(x => x.Name)
                .ToListAsync();

            var newValues = new Dictionary<int, ProductVariantAttributeValue>();

            foreach (var option in attributeOptions)
            {
                if (!existingValueNames.Contains(option.Name))
                {
                    var pvav = option.Clone();
                    pvav.ProductVariantAttributeId = productVariantAttribute.Id;

                    newValues[option.Id] = pvav;
                }
            }

            // We need the primary keys.
            await _db.ProductVariantAttributeValues.AddRangeAsync(newValues.Select(x => x.Value));

            var addedValues = await _db.SaveChangesAsync();

            // TODO: (mg) (core) Complete CopyAttributeOptionsAsync.
            //_localizedEntityService.GetLocalizedPropertyCollectionAsync(


            await _localizedEntityService.ClearCacheAsync();

            return addedValues;
        }
    }
}
