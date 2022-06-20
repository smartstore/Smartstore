using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    // TODO: (mg) (core) code-comment when finished.
    public partial class ImageImportHelper
    {
        private readonly IWebHelper _webHelper;
        private readonly IMediaService _mediaService;
        private readonly IFolderService _folderService;
        private readonly DownloadManager _downloadManager;

        // Maps downloaded URLs to file names to not download the file again.
        // Sometimes subsequent products (e.g. associated products) share the same image.
        private readonly Dictionary<string, string> _downloadUrls = new();

        // TODO: (mg) (core) better to manually pass dependencies because of MessageHandler, multi-usage of downloadManager, downloadManager.Timeout etc.
        //DataExchangeSettings dataExchangeSettings
        //_downloadManager.HttpClient.Timeout = TimeSpan.FromMinutes(dataExchangeSettings.ImageDownloadTimeout);
        public ImageImportHelper(
            IWebHelper webHelper,
            IMediaService mediaService,
            IFolderService folderService,
            DownloadManager downloadManager)
        {
            _webHelper = Guard.NotNull(webHelper, nameof(webHelper));
            _mediaService = Guard.NotNull(mediaService, nameof(mediaService));
            _folderService = Guard.NotNull(folderService, nameof(folderService));
            _downloadManager = Guard.NotNull(downloadManager, nameof(downloadManager));
        }

        /// <summary>
        /// A handler that is called when reportable events such as errors occur.
        /// </summary>
        public Action<ImportMessage, DownloadManagerItem> MessageHandler { get; init; }

        public virtual async Task<int> ImportCategoryImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            Dictionary<int, MediaFile> fileLookup = null,
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

            foreach (var item in items)
            {
                var category = item.Entity as Category;
                if (category == null)
                {
                    throw new SmartException("DownloadManagerItem does not contain the entity reference to which it belongs.");
                }

                try
                {
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
                            if (category.MediaFileId.HasValue
                                && fileLookup != null
                                && fileLookup.TryGetValue(category.MediaFileId.Value, out var assignedFile))
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
                        (fileResult.Source.State as Category).MediaFileId = fileResult.File.Id;
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
