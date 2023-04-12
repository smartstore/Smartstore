using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    [Important(HookImportance.Essential)]
    internal class TreeNodeHook : AsyncDbSaveHook<ITreeNode>
    {
        private readonly SmartDbContext _db;

        public TreeNodeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override async Task<HookResult> OnUpdatingAsync(ITreeNode entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entry.TryGetModifiedProperty(nameof(entity.ParentId), out var originalValue))
            {
                // ParentId has changed: fix TreePaths of ALL descendants.
                var oldTreePath = entity.TreePath;
                var newTreePath = entity.BuildTreePath();
                var query = entity.GetQuery(_db).ApplyDescendantsFilter(entity);

                entity.TreePath = newTreePath;

                // Replace old parent path with new parent path batch-wise.
                await query.ExecuteUpdateAsync(
                    x => x.SetProperty(p => p.TreePath, p => p.TreePath.Replace(oldTreePath, newTreePath)), cancelToken);
            }

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnInsertedAsync(ITreeNode entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var nodes = entries
                .Select(x => x.Entity)
                .OfType<ITreeNode>()
                .ToArray();

            foreach (var node in nodes)
            {
                node.TreePath = node.BuildTreePath();
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
