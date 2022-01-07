using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important(HookImportance.Essential)]
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class AuditableHook : AsyncDbSaveHook<IAuditable>
    {
        protected override Task<HookResult> OnInsertingAsync(IAuditable entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var now = DateTime.UtcNow;

            if (entity.CreatedOnUtc == DateTime.MinValue)
            {
                entity.CreatedOnUtc = now;
            }

            if (entity.UpdatedOnUtc == DateTime.MinValue)
            {
                entity.UpdatedOnUtc = now;
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(IAuditable entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            entity.UpdatedOnUtc = DateTime.UtcNow;

            return Task.FromResult(HookResult.Ok);
        }
    }
}
