using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Discounts
{
    internal class DiscountUsageHistoryHook(IRequestCache requestCache) : AsyncDbSaveHook<DiscountUsageHistory>
    {
        private readonly IRequestCache _requestCache = requestCache;

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            _requestCache.RemoveByPattern(DiscountService.DiscountsPatternKey);

            return Task.CompletedTask;
        }
    }
}
