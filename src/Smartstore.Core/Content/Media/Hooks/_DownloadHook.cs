using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Content.Media.Hooks
{
    internal class DownloadHook : AsyncDbSaveHook<BaseEntity>
    {
        // TODO: (mg) add more entity types with assigned downloads.
        private static readonly HashSet<Type> _candidateTypes = new(new Type[]
        {
            typeof(Product),
        });

        private readonly HashSet<int> _toDelete = new();

        private readonly SmartDbContext _db;

        public DownloadHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnUpdatingAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var type = entry.EntityType;

            if (!_candidateTypes.Contains(type))
            {
                return Task.FromResult(HookResult.Void);
            }

            if (entry.State == EState.Modified)
            {
                if (type == typeof(Product))
                {
                    var prop = entry.Entry.Property(nameof(Product.SampleDownloadId));
                    var oldDownloadId = prop.OriginalValue != null ? (int)prop.OriginalValue : 0;
                    var newDownloadId = prop.CurrentValue != null ? (int)prop.CurrentValue : 0;

                    if (oldDownloadId != 0 && oldDownloadId != newDownloadId)
                    {
                        _toDelete.Add(oldDownloadId);
                    }
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatedAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_toDelete.Count > 0)
            {
                var num = await _db.Downloads
                    .Where(x => _toDelete.Contains(x.Id))
                    .ExecuteDeleteAsync(cancelToken);

                $"- deleted downloads {num} {string.Join(",", _toDelete)}".Dump();
            }
        }
    }
}
