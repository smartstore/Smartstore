using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Security
{
    [Important]
    internal class PermissionRecordHook : AsyncDbSaveHook<PermissionRecord>
    {
        private readonly ICacheManager _cache;

        public PermissionRecordHook(ICacheManager cache)
        {
            _cache = cache;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            await _cache.RemoveAsync(PermissionService.PERMISSION_TREE_PATTERN_KEY);
        }
    }
}
