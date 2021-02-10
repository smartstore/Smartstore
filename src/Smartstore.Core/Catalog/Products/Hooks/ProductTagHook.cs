using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products.Hooks
{
    public class ProductTagHook : AsyncDbSaveHook<ProductTag>
    {
        private readonly ICacheManager _cache;

        public ProductTagHook(ICacheManager cache)
        {
            _cache = cache;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            await _cache.RemoveByPatternAsync(ProductTagService.PRODUCTTAG_PATTERN_KEY);
        }
    }
}
