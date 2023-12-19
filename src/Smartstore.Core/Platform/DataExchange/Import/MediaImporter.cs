using Smartstore.Collections;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Import.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class MediaImporter : IMediaImporter
    {
        const int MaxCachedDownloadUrls = 2000;

        // Maps downloaded URLs to file names. Avoids redundant downloads.
        // Sometimes subsequent products (e.g. associated products) share the same image.
        private readonly Dictionary<string, string> _downloadUrls = new();

        private readonly SmartDbContext _db;
        private readonly IWebHelper _webHelper;
        private readonly IMediaService _mediaService;
        private readonly IFolderService _folderService;
        private readonly DownloadManager _downloadManager;

        public MediaImporter(
            SmartDbContext db,
            IWebHelper webHelper,
            IMediaService mediaService,
            IFolderService folderService,
            DownloadManager downloadManager,
            DataExchangeSettings dataExchangeSettings)
        {
            _db = db;
            _webHelper = webHelper;
            _mediaService = mediaService;
            _folderService = folderService;
            _downloadManager = downloadManager;

            _downloadManager.HttpClient.Timeout = TimeSpan.FromMinutes(dataExchangeSettings.ImageDownloadTimeout);
        }

        public Action<ImportMessage, DownloadManagerItem> MessageHandler { get; set; }

        public virtual DownloadManagerItem CreateDownloadItem(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            BaseEntity entity,
            string urlOrPath,
            object state = null,
            int displayOrder = 0,
            HashSet<string> fileNameLookup = null,
            int maxFileNameLength = int.MaxValue)
        {
            Guard.NotNull(imageDirectory);
            Guard.NotNull(downloadDirectory);

            if (urlOrPath.IsEmpty())
            {
                return null;
            }

            var item = new DownloadManagerItem
            {
                Entity = entity,
                Id = displayOrder,
                DisplayOrder = displayOrder,
                State = state
            };

            try
            {
                if (urlOrPath.IsWebUrl())
                {
                    // We append quality to avoid importing of image duplicates.
                    item.Url = _webHelper.ModifyQueryString(urlOrPath, "q=100");

                    if (_downloadUrls.TryGetValue(urlOrPath, out string url))
                    {
                        // URL has already been downloaded.
                        item.Success = true;
                        item.FileName = url;
                    }
                    else
                    {
                        item.FileName = GetFileName(WebHelper.GetFileNameFromUrl(urlOrPath), maxFileNameLength, fileNameLookup);

                        fileNameLookup?.Add(item.FileName);
                    }

                    item.Path = GetAbsolutePath(downloadDirectory, item.FileName);
                }
                else
                {
                    item.FileName = GetFileName(Path.GetFileName(urlOrPath), maxFileNameLength);
                    item.Success = true;

                    item.Path = Path.IsPathRooted(urlOrPath)
                        ? urlOrPath
                        : GetAbsolutePath(imageDirectory, urlOrPath);
                }

                item.MimeType = MimeTypes.MapNameToMimeType(item.FileName);

                return item;
            }
            catch
            {
                InvokeMessageHandler($"Failed to prepare image download for '{urlOrPath}'. Skipping file.", item, ImportMessageType.Error);
                return null;
            }

            static string GetAbsolutePath(IDirectory directory, string fileNameOrRelativePath)
            {
                return Path.Combine(directory.PhysicalPath, fileNameOrRelativePath.TrimStart(PathUtility.PathSeparators).Replace('/', '\\'));
            }
        }

        #region Generic importers

        public virtual async Task<int> ImportMediaFilesManyAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            MediaFolderNode album,
            Multimap<int, IMediaFile> existingFiles,
            Func<MediaFile, DownloadManagerItem, IMediaFile> assignMediaFileHandler,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(scope);
            Guard.NotNull(album);
            Guard.NotNull(existingFiles);
            Guard.NotNull(assignMediaFileHandler);

            var itemsMap = items
                ?.Where(x => x?.Entity != null)
                ?.ToMultimap(x => x.Entity.Id, x => x);

            if (itemsMap.IsNullOrEmpty())
            {
                return 0;
            }

            var newFiles = new Dictionary<string, FileBatchSource>(StringComparer.OrdinalIgnoreCase);
            var downloadedItems = new Dictionary<string, DownloadManagerItem>();

            foreach (var pair in itemsMap)
            {
                try
                {
                    var entityId = pair.Key;
                    var productItems = pair.Value;
                    var maxDisplayOrder = int.MaxValue;

                    // Be kind and assign a continuous DisplayOrder if none has been explicitly specified by the caller.
                    if (productItems.All(x => x.DisplayOrder == 0))
                    {
                        maxDisplayOrder = existingFiles.TryGetValues(entityId, out var pmf) && pmf.Count > 0
                            ? pmf.Select(x => x.DisplayOrder).Max()
                            : 0;
                    }

                    // Download images per product.
                    await Download(productItems.Where(x => x.Url.HasValue() && !x.Success).ToArray(), downloadedItems, cancelToken);

                    foreach (var item in productItems.OrderBy(x => x.DisplayOrder))
                    {
                        if (!Succeeded(item))
                        {
                            continue;
                        }

                        using var file = new MediaImporterFile();
                        if (!file.Init(item, newFiles))
                        {
                            continue;
                        }

                        var currentFiles = existingFiles.ContainsKey(item.Entity.Id)
                            ? existingFiles[item.Entity.Id]
                            : Enumerable.Empty<IMediaFile>();

                        var equalityCheck = await _mediaService.FindEqualFileAsync(file, currentFiles.Select(x => x.MediaFile), true);
                        if (equalityCheck.Success)
                        {
                            // INFO: may occur during a initial import when products have the same SKU and
                            // the first product was overwritten with the data of the second one.
                            InvokeMessageHandler($"Found equal file in product data for '{item.FileName}'. Skipping file.", item, reason: ImportMessageReason.EqualFile);
                        }
                        else
                        {
                            if (maxDisplayOrder != int.MaxValue)
                            {
                                item.DisplayOrder = ++maxDisplayOrder;
                            }

                            equalityCheck = await _mediaService.FindEqualFileAsync(file, item.FileName, album.Id, true);
                            if (equalityCheck.Success)
                            {
                                // INFO: may occur during a subsequent import when products have the same SKU and
                                // the images of the second product are additionally assigned to the first one.
                                var assignedFile = assignMediaFileHandler(equalityCheck.Value, item);
                                existingFiles.Add(item.Entity.Id, assignedFile);
                                InvokeMessageHandler($"Found equal file in {album.Name} album for '{item.FileName}'. Assigning existing file instead.", item, reason: ImportMessageReason.EqualFileInAlbum);
                            }
                            else
                            {
                                file.Add(item, newFiles);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    InvokeMessageHandler(ex.ToAllMessages(), null, ImportMessageType.Warning);
                }
            }

            if (newFiles.Count > 0)
            {
                var postProcessingEnabled = _mediaService.ImagePostProcessingEnabled;

                try
                {
                    // Always turn image post-processing off during imports. It can heavily decrease processing time.
                    _mediaService.ImagePostProcessingEnabled = false;
                    
                    var batchResult = await _mediaService.BatchSaveFilesAsync(newFiles.Values.ToArray(), album, false, duplicateFileHandling, cancelToken);

                    foreach (var fileResult in batchResult)
                    {
                        if (fileResult.Exception == null && fileResult.File?.Id > 0)
                        {
                            var assignedItems = fileResult.Source.State as List<DownloadManagerItem>;
                            foreach (var item in assignedItems)
                            {
                                var assignedFile = assignMediaFileHandler(fileResult.File.File, item);
                                existingFiles.Add(item.Entity.Id, assignedFile);
                            }
                        }
                    }
                }
                finally
                {
                    _mediaService.ImagePostProcessingEnabled = postProcessingEnabled;
                }
            }

            await scope.CommitAsync(cancelToken);

            return newFiles.Count;
        }

        public virtual async Task<int> ImportMediaFilesAsync<T>(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            MediaFolderNode album,
            Action<DownloadManagerItem, int> assignMediaFileHandler,
            Func<DownloadManagerItem, Stream, Task<bool>> checkAssignedMediaFileHandler,
            bool checkExistingFile,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default) where T : BaseEntity
        {
            Guard.NotNull(scope);
            Guard.NotNull(album);
            Guard.NotNull(assignMediaFileHandler);
            Guard.NotNull(checkAssignedMediaFileHandler);

            items = items?.Where(x => x != null)?.ToArray();
            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            var newFiles = new Dictionary<string, FileBatchSource>(StringComparer.OrdinalIgnoreCase);
            var downloadedItems = new Dictionary<string, DownloadManagerItem>();

            foreach (var item in items)
            {
                try
                {
                    var entity = item.Entity as T;

                    if (item.Url.HasValue() && !item.Success)
                    {
                        await Download(new[] { item }, downloadedItems, cancelToken);
                    }

                    if (!Succeeded(item))
                    {
                        continue;
                    }

                    using var file = new MediaImporterFile();
                    if (!file.Init(item, newFiles))
                    {
                        continue;
                    }

                    // Check for already assigned files.
                    if (await checkAssignedMediaFileHandler(item, file))
                    {
                        InvokeMessageHandler($"Found equal file for {nameof(entity)} '{item.FileName}'. Skipping file.", item, reason: ImportMessageReason.EqualFile);
                        continue;
                    }

                    if (checkExistingFile)
                    {
                        var equalityCheck = await _mediaService.FindEqualFileAsync(file, item.FileName, album.Id, true);
                        if (equalityCheck.Success)
                        {
                            assignMediaFileHandler(item, equalityCheck.Value.Id);
                            InvokeMessageHandler($"Found equal file in {album.Name} album for '{item.FileName}'. Assigning existing file instead.", item, reason: ImportMessageReason.EqualFileInAlbum);
                            continue;
                        }
                    }

                    file.Add(item, newFiles);
                }
                catch (Exception ex)
                {
                    InvokeMessageHandler(ex.ToAllMessages(), null, ImportMessageType.Warning);
                }
            }

            if (newFiles.Count > 0)
            {
                var postProcessingEnabled = _mediaService.ImagePostProcessingEnabled;

                try
                {
                    // Always turn image post-processing off during imports. It can heavily decrease processing time.
                    _mediaService.ImagePostProcessingEnabled = false;

                    var batchResult = await _mediaService.BatchSaveFilesAsync(newFiles.Values.ToArray(), album, false, duplicateFileHandling, cancelToken);

                    foreach (var fileResult in batchResult)
                    {
                        if (fileResult.Exception == null && fileResult.File?.Id > 0)
                        {
                            // Assign MediaFile to corresponding entity via callback.
                            var assignedItems = fileResult.Source.State as List<DownloadManagerItem>;
                            foreach (var item in assignedItems)
                            {
                                assignMediaFileHandler(item, fileResult.File.Id);
                            }
                        }
                    }
                }
                finally
                {
                    _mediaService.ImagePostProcessingEnabled = postProcessingEnabled;
                }
            }

            await scope.CommitAsync(cancelToken);

            return newFiles.Count;
        }

        #endregion

        #region Specific importers

        public async Task<int> ImportProductImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            var itemIds = items
                ?.Where(x => x?.Entity != null)
                ?.ToDistinctArray(x => x.Entity.Id);

            if (itemIds.IsNullOrEmpty())
            {
                return 0;
            }

            var files = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .Where(x => itemIds.Contains(x.ProductId))
                .ToListAsync(cancelToken);

            var existingFiles = files.ToMultimap(x => x.ProductId, x => x as IMediaFile);
            var album = _folderService.GetNodeByPath(SystemAlbumProvider.Catalog).Value;

            return await ImportMediaFilesManyAsync(
                scope, 
                items,
                album,
                existingFiles,
                AssignProductMediaFile,
                duplicateFileHandling,
                cancelToken);

            IMediaFile AssignProductMediaFile(MediaFile file, DownloadManagerItem item)
            {
                var productMediaFile = new ProductMediaFile
                {
                    ProductId = item.Entity.Id,
                    MediaFileId = file.Id,
                    DisplayOrder = item.DisplayOrder
                };

                scope.DbContext.Add(productMediaFile);

                productMediaFile.MediaFile = file;

                // Update for FixProductMainPictureIds.
                ((Product)item.Entity).UpdatedOnUtc = DateTime.UtcNow;

                return productMediaFile;
            }
        }

        public async Task<int> ImportProductImagesAsync(
            Product product,
            ICollection<FileBatchSource> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(product);

            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            await _db.LoadCollectionAsync(product, x => x.ProductMediaFiles, false, q => q.Include(x => x.MediaFile), cancelToken);

            var assigments = product.ProductMediaFiles;
            var newFiles = new List<FileBatchSource>();
            var newAssigments = new List<ProductMediaFile>();
            var album = _folderService.GetNodeByPath(SystemAlbumProvider.Catalog).Value;
            var displayOrder = assigments.Count > 0 ? assigments.Max(x => x.DisplayOrder) : 0;

            foreach (var item in items)
            {
                Guard.NotEmpty(item.FileName);

                if (item.State is Dictionary<string, object> customData
                    && customData.TryGetValue(nameof(ProductMediaFile.MediaFileId), out var rawFileId)
                    && rawFileId is int fileId)
                {
                    // Overwrite existing file.
                    var file = assigments.FirstOrDefault(x => x.MediaFileId == fileId);
                    if (file != null)
                    {
                        var info = _mediaService.ConvertMediaFile(file.MediaFile);
                        var updatedFile = await _mediaService.SaveFileAsync(info.Path, item.Source.SourceStream, false, DuplicateFileHandling.Overwrite);

                        if (updatedFile == null || updatedFile.Id != fileId)
                        {
                            InvokeMessageHandler($"Failed to update existing product file with ID {fileId} and path '{info.Path.NaIfEmpty()}'.", null, ImportMessageType.Error);
                        }
                        continue;
                    }
                }

                var equalityCheck = await _mediaService.FindEqualFileAsync(item.Source.SourceStream, assigments.Select(x => x.MediaFile), true);
                if (!equalityCheck.Success)
                {
                    equalityCheck = await _mediaService.FindEqualFileAsync(item.Source.SourceStream, item.FileName, album.Id, true);
                    if (equalityCheck.Success)
                    {
                        if (!assigments.Any(x => x.MediaFileId == equalityCheck.Value.Id))
                        {
                            newAssigments.Add(new ProductMediaFile
                            {
                                ProductId = product.Id,
                                MediaFileId = equalityCheck.Value.Id,
                                DisplayOrder = ++displayOrder
                            });
                        }
                    }
                    else
                    {
                        newFiles.Add(item);
                    }
                }
            }

            if (newFiles.Count > 0)
            {
                var postProcessingEnabled = _mediaService.ImagePostProcessingEnabled;

                try
                {
                    var batchFileResult = await _mediaService.BatchSaveFilesAsync(
                        newFiles.ToArray(),
                        album,
                        false,
                        duplicateFileHandling,
                        cancelToken);

                    batchFileResult
                        .Where(x => x.Exception == null && x.File?.Id > 0)
                        .Each(x => newAssigments.Add(new ProductMediaFile
                        {
                            ProductId = product.Id,
                            MediaFileId = x.File!.Id,
                            DisplayOrder = ++displayOrder
                        }));
                }
                finally
                {
                    _mediaService.ImagePostProcessingEnabled = postProcessingEnabled;
                }
            }

            if (newAssigments.Count > 0)
            {
                // INFO: FixProductMainPictureId is called by ProductMediaFileHook.
                assigments.AddRange(newAssigments);

                await _db.SaveChangesAsync(cancelToken);
            }

            return newFiles.Count;
        }

        public async Task<int> ImportCategoryImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            var itemsArr = items?.Where(x => x != null)?.ToArray();
            if (itemsArr.IsNullOrEmpty())
            {
                return 0;
            }

            var existingFileIds = itemsArr
                .Select(x => x.Entity as Category)
                .Where(x => x != null && x.MediaFileId > 0)
                .ToDistinctArray(x => x.MediaFileId.Value);

            var files = await _mediaService.GetFilesByIdsAsync(existingFileIds);
            var existingFiles = files.ToDictionary(x => x.Id, x => x.File);

            return await ImportMediaFilesAsync<Category>(
                scope,
                items,
                _folderService.GetNodeByPath(SystemAlbumProvider.Catalog).Value,
                AssignMediaFile,
                CheckAssignedFile,
                true,
                duplicateFileHandling,
                cancelToken);

            async Task<bool> CheckAssignedFile(DownloadManagerItem item, Stream stream)
            {
                var category = (Category)item.Entity;
                if (category.MediaFileId.HasValue && existingFiles.TryGetValue(category.MediaFileId.Value, out var assignedFile))
                {
                    var isEqualData = await _mediaService.FindEqualFileAsync(stream, new[] { assignedFile }, true);
                    if (isEqualData.Success)
                    {
                        return true;
                    }
                }

                return false;
            }

            static void AssignMediaFile(DownloadManagerItem item, int fileId)
            {
                ((Category)item.Entity).MediaFileId = fileId;
            }
        }

        public async Task<int> ImportCustomerAvatarsAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            return await ImportMediaFilesAsync<Customer>(
                scope, 
                items,
                _folderService.GetNodeByPath(SystemAlbumProvider.Customers).Value,
                AddCustomerAvatarMediaFile,
                CheckAssignedFile,
                false,
                duplicateFileHandling,
                cancelToken);

            async Task<bool> CheckAssignedFile(DownloadManagerItem item, Stream stream)
            {
                var customer = (Customer)item.Entity;
                var file = await _mediaService.GetFileByIdAsync(customer.GenericAttributes.AvatarPictureId ?? 0, MediaLoadFlags.AsNoTracking);
                if (file != null)
                {
                    var isEqualData = await _mediaService.FindEqualFileAsync(stream, new[] { file.File }, true);
                    if (isEqualData.Success)
                    {
                        return true;
                    }
                }

                return false;
            }

            static void AddCustomerAvatarMediaFile(DownloadManagerItem item, int fileId)
            {
                ((Customer)item.Entity).GenericAttributes.Set(SystemCustomerAttributeNames.AvatarPictureId, fileId);
            }
        }

        #endregion

        #region Utilities

        private async Task Download(DownloadManagerItem[] items, Dictionary<string, DownloadManagerItem> downloadedItems, CancellationToken cancelToken)
        {
            if (items.Length == 0)
            {
                return;
            }

            // Exclude items that have already been downloaded within the current batch.
            // Avoids possible IOException when DownloadManager accesses the file.
            if (downloadedItems.Count > 0 && items.Any(x => !x.Success && downloadedItems.ContainsKey(x.Url)))
            {
                foreach (var item in items)
                {
                    if (!item.Success && downloadedItems.TryGetValue(item.Url, out var downloadedItem))
                    {
                        item.Success = downloadedItem.Success;
                        item.FileName = downloadedItem.FileName;
                        item.Path = downloadedItem.Path;
                    }
                }

                items = items.Where(x => !x.Success).ToArray();
                if (items.Length == 0)
                {
                    return;
                }
            }

            // TODO: (mg) (core) Make this fire&forget somehow and sync later.
            // RE: I do not see any extra benefit in this specific case.

            //$"- download {string.Join(',', items.Select(x => x.Url))}".Dump();
            await _downloadManager.DownloadFilesAsync(items, cancelToken);

            foreach (var item in items)
            {
                if (item.Success && File.Exists(item.Path))
                {
                    // Cache URL to avoid redundant downloads over multiple batches.
                    if (!_downloadUrls.ContainsKey(item.Url))
                    {
                        if (_downloadUrls.Count >= MaxCachedDownloadUrls)
                        {
                            _downloadUrls.Clear();
                        }

                        _downloadUrls[item.Url] = Path.GetFileName(item.Path);
                    }

                    downloadedItems[item.Url] = item;
                }
                else
                {
                    InvokeMessageHandler($"Download failed for {item.Url}.", item, reason: ImportMessageReason.DownloadFailed);
                }
            }
        }

        protected virtual void InvokeMessageHandler(
            string msg,
            DownloadManagerItem item = null,
            ImportMessageType messageType = ImportMessageType.Info,
            ImportMessageReason reason = ImportMessageReason.None)
        {
            if (MessageHandler != null && msg.HasValue())
            {
                MessageHandler.Invoke(new ImportMessage(msg, messageType, reason)
                {
                    AffectedField = item != null ? $"{item.Entity.GetEntityName()} #{item.DisplayOrder}" : null
                },
                item);
            }
        }

        private bool Succeeded(DownloadManagerItem item)
        {
            if (item.Entity == null)
            {
                InvokeMessageHandler($"{nameof(DownloadManagerItem)} does not contain the entity to which it belongs.", item, ImportMessageType.Error);
                return false;
            }

            if (!item.Success && item.ErrorMessage.HasValue())
            {
                InvokeMessageHandler(item.ToString(), item, ImportMessageType.Error);
                return false;
            }

            return item.Success;
        }

        private static string GetFileName(string fName, int maxLength = int.MaxValue, HashSet<string> lookup = null)
        {
            if (fName.IsEmpty())
            {
                return Path.GetRandomFileName();
            }

            fName = PathUtility.SanitizeFileName(fName);

            if (maxLength == int.MaxValue && lookup == null)
            {
                return fName;
            }

            var name = Path.GetFileNameWithoutExtension(fName);
            var ext = Path.GetExtension(fName);

            if (name.Length > maxLength)
            {
                name = name.Truncate(maxLength);
                fName = name + ext;
            }

            if (lookup?.Contains(fName) ?? false)
            {
                var i = 0;
                do
                {
                    fName = $"{name}-{++i}{ext}";
                }
                while (lookup.Contains(fName));
            }

            return fName;
        }

        #endregion
    }
}