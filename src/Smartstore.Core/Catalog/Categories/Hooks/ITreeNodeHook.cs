//using Smartstore.Core.Data;
//using Smartstore.Data.Hooks;

//namespace Smartstore.Core.Catalog.Categories
//{
//    [Important(HookImportance.Essential)]
//    internal class TreeNodeHook : AsyncDbSaveHook<ITreeNode>
//    {
//        private readonly SmartDbContext _db;
//        private readonly HashSet<ITreeNode> _toUpdate;
        
//        public TreeNodeHook(SmartDbContext db)
//        {
//            _db = db;
//        }

//        protected override Task<HookResult> OnUpdatingAsync(ITreeNode entity, IHookedEntity entry, CancellationToken cancelToken)
//        {
//            if (entry.IsPropertyModified(nameof(entity.ParentId)))
//            {
//                _toUpdate.Add(entity);
//            }

//            return Task.FromResult(HookResult.Ok);
//        }

//        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
//        {
//            _toUpdate.Clear();
//            return base.OnBeforeSaveCompletedAsync(entries, cancelToken);
//        }

//        protected override Task<HookResult> OnInsertedAsync(ITreeNode entity, IHookedEntity entry, CancellationToken cancelToken)
//        {
//            return Task.FromResult(HookResult.Ok);
//        }

//        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
//        {
//            _toUpdate.Clear();
//            return base.OnAfterSaveCompletedAsync(entries, cancelToken);
//        }
//    }
//}
