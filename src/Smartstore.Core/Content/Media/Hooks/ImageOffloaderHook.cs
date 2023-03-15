//using Smartstore.Core.Catalog.Brands;
//using Smartstore.Core.Catalog.Categories;
//using Smartstore.Core.Catalog.Products;
//using Smartstore.Core.Content.Topics;
//using Smartstore.Core.Data;
//using Smartstore.Data.Hooks;

//namespace Smartstore.Core.Content.Media.Hooks
//{
//    internal class ImageOffloaderHook : AsyncDbSaveHook<BaseEntity>
//    {
//        private readonly Lazy<IImageOffloder> _imageOffloader;
//        private readonly HashSet<BaseEntity> _toProcess = new();

//        public ImageOffloaderHook(Lazy<IImageOffloder> imageOffloader)
//        {
//            _imageOffloader = imageOffloader;
//        }

//        private static bool IsValidEntry(IHookedEntity entry)
//        {
//            if (entry.InitialState == EntityState.Deleted)
//            {
//                return false;
//            }

//            var t = entry.EntityType;
//            return t == typeof(Product) || t == typeof(Category) || t == typeof(Manufacturer) || t == typeof(Topic);
//        }

//        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
//        {
//            if (!IsValidEntry(entry))
//            {
//                return Task.FromResult(HookResult.Void);
//            }

//            if (entry.InitialState == EntityState.Added)
//            {
//                _toProcess.Add(entry.Entity);
//            }
//            else if (entry.InitialState == EntityState.Modified)
//            {
//                var modifiedProps = entry.Entry.GetModifiedProperties();
//                var type = entry.EntityType;
//                var isModified = false;

//                if (isModified)
//                {
//                    _toProcess.Add(entry.Entity);
//                }
//            }

//            return Task.FromResult(HookResult.Ok);
//        }

//        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
//            => Task.FromResult(IsValidEntry(entry) ? HookResult.Ok : HookResult.Void);

//        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
//        {
//            if (_toProcess.Count == 0)
//            {
//                return;
//            }

//            var offloader = _imageOffloader.Value;
//            var folder = await offloader.GetDefaultMediaFolderAsync();

//            foreach (var grp in _toProcess.GroupBy(x => x.GetType()))
//            {
//                foreach (var entity in grp)
//                {
//                    await Task.Delay(10);
//                }
//            }
//        }
//    }
//}
