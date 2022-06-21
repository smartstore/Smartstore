using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    /// <summary>
    /// Helper to import product and category images in a performant way.
    /// Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified and
    /// they will not be imported if they already exist (duplicate check).
    /// </summary>
    public partial class ImageImportHelper
    {
        private readonly SmartDbContext _db;
        private readonly IWebHelper _webHelper;
        private readonly IMediaService _mediaService;
        private readonly IFolderService _folderService;
        private readonly DownloadManager _downloadManager;

        // Maps downloaded URLs to file names to not download the file again.
        // Sometimes subsequent products (e.g. associated products) share the same image.
        private readonly Dictionary<string, string> _downloadUrls = new();

        public ImageImportHelper(
            SmartDbContext db,
            IWebHelper webHelper,
            IMediaService mediaService,
            IFolderService folderService,
            DownloadManager downloadManager)
        {
            _db = Guard.NotNull(db, nameof(db));
            _webHelper = Guard.NotNull(webHelper, nameof(webHelper));
            _mediaService = Guard.NotNull(mediaService, nameof(mediaService));
            _folderService = Guard.NotNull(folderService, nameof(folderService));
            _downloadManager = Guard.NotNull(downloadManager, nameof(downloadManager));
        }

        /// <summary>
        /// A handler that is called when reportable events such as errors occur.
        /// </summary>
        public Action<ImportMessage, DownloadManagerItem> MessageHandler { get; init; }

        /// <summary>
        /// Imports a batch of product images.
        /// </summary>
        /// <param name="items">Collection of product images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of saved files.</returns>
        public virtual async Task<int> ImportProductImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(scope, nameof(scope));

            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            var newFiles = new List<FileBatchSource>();
            var catalogAlbum = _folderService.GetNodeByPath(SystemAlbumProvider.Catalog).Value;
            var itemsMap = items.Where(x => x.Entity != null).ToMultimap(x => x.Entity.Id, x => x);

            var files = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .Where(x => itemsMap.Keys.Contains(x.ProductId))
                .ToListAsync(cancelToken);

            var existingFiles = files.ToMultimap(x => x.ProductId, x => x);

            foreach (var pair in itemsMap)
            {
                try
                {
                    // Download images per product.
                    if (pair.Value.Any(x => x.Url.HasValue()))
                    {
                        // TODO: (mg) (core) Make this fire&forget somehow and sync later.
                        await _downloadManager.DownloadFilesAsync(
                            pair.Value.Where(x => x.Url.HasValue() && !x.Success),
                            cancelToken);
                    }

                    foreach (var item in pair.Value.OrderBy(x => x.DisplayOrder))
                    {
                        var product = item.Entity as Product;
                        if (product == null)
                        {
                            AddMessage("DownloadManagerItem does not contain the product entity to which it belongs.", ImportMessageType.Error);
                            continue;
                        }

                        if (DownloadSucceeded(item))
                        {
                            using var stream = File.OpenRead(item.Path);

                            if (stream?.Length > 0)
                            {
                                var currentFiles = existingFiles.ContainsKey(product.Id)
                                    ? existingFiles[product.Id]
                                    : Enumerable.Empty<ProductMediaFile>();

                                var equalityCheck = await _mediaService.FindEqualFileAsync(stream, currentFiles.Select(x => x.MediaFile), true);
                                if (equalityCheck.Success)
                                {
                                    // INFO: may occur during a initial import when products have the same SKU and
                                    // the first product was overwritten with the data of the second one.
                                    AddMessage($"Found equal file in product data for '{item.FileName}'. Skipping file.");
                                }
                                else
                                {
                                    equalityCheck = await _mediaService.FindEqualFileAsync(stream, item.FileName, catalogAlbum.Id, true);
                                    if (equalityCheck.Success)
                                    {
                                        // INFO: may occur during a subsequent import when products have the same SKU and
                                        // the images of the second product are additionally assigned to the first one.
                                        AddProductMediaFile(equalityCheck.Value, item);
                                        AddMessage($"Found equal file in catalog album for '{item.FileName}'. Assigning existing file instead.");
                                    }
                                    else
                                    {
                                        // Keep path for later batch import of new images.
                                        newFiles.Add(new FileBatchSource
                                        {
                                            PhysicalPath = item.Path,
                                            FileName = item.FileName,
                                            State = item
                                        });
                                    }
                                }
                            }
                        }
                        else if (item.Url.HasValue())
                        {
                            AddMessage($"Download failed for image {item.Url}.");
                        }

                        void AddMessage(string msg, ImportMessageType messageType = ImportMessageType.Info)
                        {
                            MessageHandler?.Invoke(new ImportMessage(msg, messageType) { AffectedField = $"Product image URL #{item.DisplayOrder}" }, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageHandler?.Invoke(new ImportMessage(ex.ToAllMessages(), ImportMessageType.Warning) { AffectedField = $"Product image URL" }, null);
                }
            }

            if (newFiles.Count > 0)
            {
                var batchFileResult = await _mediaService.BatchSaveFilesAsync(
                    newFiles.ToArray(),
                    catalogAlbum,
                    false,
                    duplicateFileHandling,
                    cancelToken);

                foreach (var fileResult in batchFileResult)
                {
                    if (fileResult.Exception == null && fileResult.File?.Id > 0)
                    {
                        AddProductMediaFile(fileResult.File.File, fileResult.Source.State as DownloadManagerItem);
                    }
                }
            }

            return await scope.CommitAsync(cancelToken);

            void AddProductMediaFile(MediaFile file, DownloadManagerItem item)
            {
                var productMediaFile = new ProductMediaFile
                {
                    ProductId = item.Entity.Id,
                    MediaFileId = file.Id,
                    DisplayOrder = item.DisplayOrder
                };

                scope.DbContext.Add(productMediaFile);

                productMediaFile.MediaFile = file;
                existingFiles.Add(item.Entity.Id, productMediaFile);

                // Update for FixProductMainPictureIds.
                ((Product)item.Entity).UpdatedOnUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Imports a batch of category images.
        /// </summary>
        /// <param name="items">Collection of category images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of saved files.</returns>
        public virtual async Task<int> ImportCategoryImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(scope, nameof(scope));

            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            var newFiles = new List<FileBatchSource>();
            var catalogAlbum = _folderService.GetNodeByPath(SystemAlbumProvider.Catalog).Value;

            var existingFileIds = items
                .Select(x => x.Entity as Category)
                .Where(x => x != null && x.MediaFileId > 0)
                .ToDistinctArray(x => x.MediaFileId.Value);

            var files = await _mediaService.GetFilesByIdsAsync(existingFileIds);
            var existingFiles = files.ToDictionary(x => x.Id, x => x.File);

            foreach (var item in items)
            {
                try
                {
                    var category = item.Entity as Category;
                    if (category == null)
                    {
                        AddMessage("DownloadManagerItem does not contain the category entity to which it belongs.", ImportMessageType.Error);
                        continue;
                    }

                    // Download image.
                    if (item.Url.HasValue() && !item.Success)
                    {
                        await _downloadManager.DownloadFilesAsync(new[] { item }, cancelToken);
                    }

                    if (DownloadSucceeded(item))
                    {
                        using var stream = File.OpenRead(item.Path);

                        if (stream?.Length > 0)
                        {
                            if (category.MediaFileId.HasValue && existingFiles.TryGetValue(category.MediaFileId.Value, out var assignedFile))
                            {
                                var isEqualData = await _mediaService.FindEqualFileAsync(stream, new[] { assignedFile }, true);
                                if (isEqualData.Success)
                                {
                                    AddMessage($"Found equal file in category data for '{item.FileName}'. Skipping file.");
                                    continue;
                                }
                            }

                            var equalityCheck = await _mediaService.FindEqualFileAsync(stream, item.FileName, catalogAlbum.Id, true);
                            if (equalityCheck.Success)
                            {
                                category.MediaFileId = equalityCheck.Value.Id;
                                AddMessage($"Found equal file in catalog album for '{item.FileName}'. Assigning existing file instead.");
                            }
                            else
                            {
                                // Keep path for later batch import of new images.
                                newFiles.Add(new FileBatchSource
                                {
                                    PhysicalPath = item.Path,
                                    FileName = item.FileName,
                                    State = category
                                });
                            }
                        }
                    }
                    else if (item.Url.HasValue())
                    {
                        AddMessage($"Download failed for image {item.Url}.");
                    }
                }
                catch (Exception ex)
                {
                    AddMessage(ex.ToAllMessages(), ImportMessageType.Warning);
                }

                void AddMessage(string msg, ImportMessageType messageType = ImportMessageType.Info)
                {
                    MessageHandler?.Invoke(new ImportMessage(msg, messageType) { AffectedField = "Category image URL" }, item);
                }
            }

            if (newFiles.Count > 0)
            {
                var batchFileResult = await _mediaService.BatchSaveFilesAsync(
                    newFiles.ToArray(),
                    catalogAlbum,
                    false,
                    duplicateFileHandling,
                    cancelToken);

                foreach (var fileResult in batchFileResult)
                {
                    if (fileResult.Exception == null && fileResult.File?.Id > 0)
                    {
                        ((Category)fileResult.Source.State).MediaFileId = fileResult.File.Id;
                    }
                }
            }

            return await scope.CommitAsync(cancelToken);
        }

        /// <summary>
        /// Gets a value indicating whether the download succeeded.
        /// </summary>
        /// <param name="item">Download manager item.</param>
        /// <param name="maxCachedUrls">
        /// The maximum number of internally cached download URLs.
        /// Internal caching avoids multiple downloads of identical images.
        /// </param>
        /// <returns>A value indicating whether the download succeeded.</returns>
        public virtual bool DownloadSucceeded(DownloadManagerItem item, int maxCachedUrls = 1000)
        {
            if (item.Success && File.Exists(item.Path))
            {
                // "Cache" URL to not download it again.
                if (item.Url.HasValue() && !_downloadUrls.ContainsKey(item.Url))
                {
                    if (_downloadUrls.Count >= maxCachedUrls)
                    {
                        _downloadUrls.Clear();
                    }

                    _downloadUrls[item.Url] = Path.GetFileName(item.Path);
                }

                return true;
            }
            else
            {
                if (item.ErrorMessage.HasValue())
                {
                    MessageHandler?.Invoke(new ImportMessage(item.ToString(), ImportMessageType.Error), item);
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a download manager item.
        /// </summary>
        /// <param name="imageDirectory">
        /// Directory with images to be imported. 
        /// In that case, the images in the import file are referenced by file path (absolute or relative).
        /// </param>
        /// <param name="downloadDirectory">Directory in which the downloaded images will be saved.</param>
        /// <param name="urlOrPath">URL or path to download.</param>
        /// <param name="displayOrder">Display order of the item.</param>
        /// <returns>Download manager item.</returns>
        public virtual DownloadManagerItem CreateDownloadItem(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            string urlOrPath,
            int displayOrder)
        {
            if (urlOrPath.IsEmpty())
            {
                return null;
            }

            try
            {
                var item = CreateDownloadItemInternal(imageDirectory, downloadDirectory, urlOrPath, displayOrder, null);
                return item;
            }
            catch
            {
                MessageHandler?.Invoke(new ImportMessage($"Failed to prepare image download for '{urlOrPath}'. Skipping file.", ImportMessageType.Error), null);
                return null;
            }
        }

        /// <summary>
        /// Creates download manager items from URLs or file pathes.
        /// </summary>
        /// <param name="imageDirectory">
        /// Directory with images to be imported. 
        /// In that case, the images in the import file are referenced by file path (absolute or relative).
        /// </param>
        /// <param name="downloadDirectory">Directory in which the downloaded images will be saved.</param>
        /// <param name="urlOrPathes">URLs or pathes to download.</param>
        /// <param name="maxItems">Maximum number of returned items, <c>null</c> to return all items.</param>
        /// <returns>Download manager items.</returns>
        public virtual List<DownloadManagerItem> CreateDownloadItems(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            string[] urlOrPathes, 
            int? maxItems = null)
        {
            var items = new List<DownloadManagerItem>();
            var existingNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var itemNum = 0;

            foreach (var urlOrPath in urlOrPathes)
            {
                if (urlOrPath.HasValue())
                {
                    try
                    {
                        var item = CreateDownloadItemInternal(imageDirectory, downloadDirectory, urlOrPath, ++itemNum, existingNames);
                        items.Add(item);
                    }
                    catch
                    {
                        MessageHandler?.Invoke(new ImportMessage($"Failed to prepare image download for '{urlOrPath}'. Skipping file.", ImportMessageType.Error), null);
                    }

                    if (maxItems.HasValue && items.Count >= maxItems.Value)
                    {
                        break;
                    }
                }
            }

            return items;
        }

        private DownloadManagerItem CreateDownloadItemInternal(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            string urlOrPath,
            int displayOrder,
            HashSet<string> existingFileNames)
        {
            Guard.NotNull(imageDirectory, nameof(imageDirectory));
            Guard.NotNull(downloadDirectory, nameof(downloadDirectory));

            var item = new DownloadManagerItem();

            if (urlOrPath.IsWebUrl())
            {
                // We append quality to avoid importing of image duplicates.
                item.Url = _webHelper.ModifyQueryString(urlOrPath, "q=100", null);

                if (_downloadUrls.ContainsKey(urlOrPath))
                {
                    // URL has already been downloaded.
                    item.Success = true;
                    item.FileName = _downloadUrls[urlOrPath];
                }
                else
                {
                    var fileName = WebHelper.GetFileNameFromUrl(urlOrPath) ?? Path.GetRandomFileName();
                    item.FileName = GetUniqueFileName(fileName, existingFileNames);

                    existingFileNames?.Add(item.FileName);
                }

                item.Path = GetAbsolutePath(downloadDirectory, item.FileName);
            }
            else
            {
                item.Success = true;
                item.FileName = Path.GetFileName(urlOrPath).ToValidFileName().NullEmpty() ?? Path.GetRandomFileName();

                item.Path = Path.IsPathRooted(urlOrPath)
                    ? urlOrPath
                    : GetAbsolutePath(imageDirectory, urlOrPath);
            }

            item.MimeType = MimeTypes.MapNameToMimeType(item.FileName);
            item.Id = displayOrder;
            item.DisplayOrder = displayOrder;

            return item;

            static string GetUniqueFileName(string fileName, HashSet<string> lookup)
            {
                if (lookup?.Contains(fileName) ?? false)
                {
                    var i = 0;
                    var name = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);

                    do
                    {
                        fileName = $"{name}-{++i}{ext}";
                    }
                    while (lookup.Contains(fileName));
                }

                return fileName;
            }

            static string GetAbsolutePath(IDirectory directory, string fileNameOrRelativePath)
            {
                return directory.FileSystem.PathCombine(directory.PhysicalPath, fileNameOrRelativePath).Replace('/', '\\');
            }
        }
    }
}
