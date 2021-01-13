using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    public class UnavailableAttributeCombinationsHook : AsyncDbSaveHook<BaseEntity>
    {
        private readonly ICacheManager _cache;

        private static readonly HashSet<Type> _combinationsInvalidationTypes = new HashSet<Type>(new[]
        {
            typeof(Setting),
            typeof(Product),
            typeof(ProductVariantAttributeCombination)
        });

        public UnavailableAttributeCombinationsHook(ICacheManager cache)
        {
            _cache = cache;
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (!_combinationsInvalidationTypes.Contains(entry.EntityType))
            {
                return HookResult.Void;
            }

            var entity = entry.Entity;
            var productId = 0;

            if (entity is Setting setting)
            {
                if (setting.Name.EqualsNoCase("PerformanceSettings.MaxUnavailableAttributeCombinations"))
                {
                    await _cache.RemoveByPatternAsync(ProductAttributeParser.UNAVAILABLE_COMBINATIONS_PATTERN_KEY);
                }
            }
            else if (entity is Product)
            {
                productId = entity.Id;
            }
            else if (entity is ProductVariantAttributeCombination combination)
            {
                productId = combination.ProductId;
            }

            if (productId != 0)
            {
                await _cache.RemoveAsync(ProductAttributeParser.UNAVAILABLE_COMBINATIONS_KEY.FormatInvariant(productId));
            }

            return HookResult.Ok;
        }
    }
}
