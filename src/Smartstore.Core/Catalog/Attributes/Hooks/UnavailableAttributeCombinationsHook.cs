using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Attributes
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    internal class UnavailableAttributeCombinationsHook : AsyncDbSaveHook<BaseEntity>
    {
        private readonly ICacheManager _cache;

        private static readonly HashSet<Type> _combinationsInvalidationTypes = new(new[]
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
                if (setting.Name.EqualsNoCase(TypeHelper.NameOf<PerformanceSettings>(x => x.MaxUnavailableAttributeCombinations, true)))
                {
                    await _cache.RemoveByPatternAsync(ProductAttributeMaterializer.UanavailableCombinationsPatternKey);
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
                await _cache.RemoveAsync(ProductAttributeMaterializer.UnavailableCombinationsKey.FormatInvariant(productId));
            }

            return HookResult.Ok;
        }
    }
}
