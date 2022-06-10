using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Security
{
    [Important]
    [ServiceLifetime(ServiceLifetime.Singleton)]
    internal class PermissionRoleMappingHook : AsyncDbSaveHook<PermissionRoleMapping>
    {
        private readonly ICacheManager _cache;

        public PermissionRoleMappingHook(ICacheManager cache)
        {
            _cache = cache;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            await _cache.RemoveByPatternAsync(PermissionService.PERMISSION_TREE_PATTERN_KEY);
        }
    }
}
