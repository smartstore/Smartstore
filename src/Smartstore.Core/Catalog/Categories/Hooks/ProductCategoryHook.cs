using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    public class ProductManufacturerHook : AsyncDbSaveHook<ProductCategory>
    {
        private readonly IRequestCache _requestCache;

        public ProductManufacturerHook(IRequestCache requestCache)
        {
            _requestCache = requestCache;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            _requestCache.RemoveByPattern(CategoryService.PRODUCTCATEGORIES_PATTERN_KEY);

            return Task.CompletedTask;
        }
    }
}
