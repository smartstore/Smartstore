using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Brands
{
    public class ProductManufacturerHook : AsyncDbSaveHook<ProductManufacturer>
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
            _requestCache.RemoveByPattern(ManufacturerService.PRODUCTMANUFACTURERS_PATTERN_KEY);

            return Task.CompletedTask;
        }
    }
}
