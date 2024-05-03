using System.Buffers;
using System.Drawing;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.WebUtilities;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Data;
using Smartstore.Events;
using Smartstore.Imaging;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaService : IMediaService
    {
        private readonly SmartDbContext _db;
        private readonly IFolderService _folderService;
        private readonly IMediaSearcher _searcher;
        private readonly IMediaTypeResolver _typeResolver;
        private readonly IMediaUrlGenerator _urlGenerator;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MediaSettings _mediaSettings;
        private readonly IImageProcessor _imageProcessor;
        private readonly IImageCache _imageCache;
        private readonly MediaExceptionFactory _exceptionFactory;
        private readonly IMediaDupeDetectorFactory _dupeDetectorFactory;
        private readonly IMediaStorageProvider _storageProvider;
        private readonly MediaHelper _helper;

        public MediaService(
            SmartDbContext db,
            IFolderService folderService,
            IMediaSearcher searcher,
            IMediaTypeResolver typeResolver,
            IMediaUrlGenerator urlGenerator,
            IEventPublisher eventPublisher,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            MediaSettings mediaSettings,
            IImageProcessor imageProcessor,
            IImageCache imageCache,
            MediaExceptionFactory exceptionFactory,
            IMediaDupeDetectorFactory dupeDetectorFactory,
            Func<IMediaStorageProvider> storageProvider,
            MediaHelper helper)
        {
            _db = db;
            _folderService = folderService;
            _searcher = searcher;
            _typeResolver = typeResolver;
            _urlGenerator = urlGenerator;
            _eventPublisher = eventPublisher;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _mediaSettings = mediaSettings;
            _imageProcessor = imageProcessor;
            _imageCache = imageCache;
            _exceptionFactory = exceptionFactory;
            _dupeDetectorFactory = dupeDetectorFactory;
            _storageProvider = storageProvider();
            _helper = helper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public IMediaStorageProvider StorageProvider => _storageProvider;

        public bool ImagePostProcessingEnabled { get; set; } = true;

        #region Query

        public async Task<int> CountFilesAsync(MediaSearchQuery query)
        {
            Guard.NotNull(query);

            var q = _searcher.PrepareQuery(query, MediaLoadFlags.None);
            return await q.CountAsync();
        }

        public async Task<FileCountResult> CountFilesGroupedAsync(MediaFilesFilter filter)
        {
            // TODO: (core) Throws
            Guard.NotNull(filter);

            // Base db query
            var q = _searcher.ApplyFilterQuery(filter);

            // Get ids of untrackable folders, 'cause no orphan check can be made for them.
            var untrackableFolderIds = _folderService.GetRootNode()
                .SelectNodes(x => !x.Value.CanDetectTracks)
                .Select(x => x.Value.Id)
                .ToArray();

            // Determine counts
            var result = await (from f in q
                                group f by 1 into g
                                orderby 1
                                select new FileCountResult
                                {
                                    Total = g.Count(),
                                    Trash = g.Count(x => x.Deleted),
                                    Unassigned = g.Count(x => !x.Deleted && x.FolderId == null),
                                    Transient = g.Count(x => !x.Deleted && x.IsTransient == true),
                                    //Orphan = g.Count(x => !x.Deleted && x.FolderId > 0 && !untrackableFolderIds.Contains(x.FolderId.Value) && !x.Tracks.Any())
                                }).FirstOrDefaultAsync() ?? new FileCountResult();

            if (result.Total == 0)
            {
                result.Folders = new Dictionary<int, int>();
                return result;
            }

            // Cannot be executed on the server by the above query.
            result.Orphan = await q
                .Where(x => !x.Deleted && x.FolderId > 0 && !untrackableFolderIds.Contains(x.FolderId.Value) && !x.Tracks.Any())
                .CountAsync();

            // Determine file count for each folder
            var byFoldersQuery = from f in q
                                 where f.FolderId > 0 && !f.Deleted
                                 group f by f.FolderId.Value into grp
                                 select new
                                 {
                                     FolderId = grp.Key,
                                     Count = grp.Count()
                                 };

            result.Folders = await byFoldersQuery.ToDictionaryAsync(k => k.FolderId, v => v.Count);
            result.Filter = filter;

            return result;
        }

        public async Task<MediaSearchResult> SearchFilesAsync(
            MediaSearchQuery query,
            Func<IQueryable<MediaFile>, IQueryable<MediaFile>> queryModifier,
            MediaLoadFlags flags = MediaLoadFlags.AsNoTracking)
        {
            Guard.NotNull(query);

            var files = _searcher.SearchFiles(query, flags);
            if (queryModifier != null)
            {
                files.ModifyQuery(queryModifier);
            }

            return new MediaSearchResult(await files.LoadAsync(), ConvertMediaFile);
        }

        #endregion

        #region Read

        public async Task<bool> FileExistsAsync(string path)
        {
            Guard.NotEmpty(path);

            if (_helper.TokenizePath(path, false, out var tokens))
            {
                return await _db.MediaFiles.AnyAsync(x => x.FolderId == tokens.Folder.Id && x.Name == tokens.FileName);
            }

            return false;
        }

        public async Task<MediaFileInfo> GetFileByPathAsync(string path, MediaLoadFlags flags = MediaLoadFlags.None)
        {
            Guard.NotEmpty(path);

            if (_helper.TokenizePath(path, false, out var tokens))
            {
                var table = _searcher.ApplyLoadFlags(_db.MediaFiles, flags);

                var entity = await table.FirstOrDefaultAsync(x => x.FolderId == tokens.Folder.Id && x.Name == tokens.FileName);
                if (entity != null)
                {
                    await EnsureMetadataResolvedAsync(entity, true);
                    return ConvertMediaFile(entity, tokens.Folder);
                }
            }

            return null;
        }

        public async Task<MediaFileInfo> GetFileByIdAsync(int id, MediaLoadFlags flags = MediaLoadFlags.None)
        {
            if (id <= 0)
                return null;

            MediaFile entity = null;

            if (flags == MediaLoadFlags.None)
            {
                entity = await _db.MediaFiles.FindByIdAsync(id);
            }
            else
            {
                var query = _db.MediaFiles.Where(x => x.Id == id);
                entity = await _searcher.ApplyLoadFlags(query, flags).FirstOrDefaultAsync();
            }

            if (entity != null)
            {
                await EnsureMetadataResolvedAsync(entity, true);
                return ConvertMediaFile(entity, _folderService.FindNode(entity)?.Value);
            }

            return null;
        }

        public async Task<MediaFileInfo> GetFileByNameAsync(int folderId, string fileName, MediaLoadFlags flags = MediaLoadFlags.None)
        {
            Guard.IsPositive(folderId);
            Guard.NotEmpty(fileName);

            var query = _db.MediaFiles.Where(x => x.Name == fileName && x.FolderId == folderId);
            var entity = await _searcher.ApplyLoadFlags(query, flags).FirstOrDefaultAsync();

            if (entity != null)
            {
                await EnsureMetadataResolvedAsync(entity, true);
                var node = _folderService.FindNode(entity);
                var dir = node?.Value?.Path;
                return ConvertMediaFile(entity, node?.Value);
            }

            return null;
        }

        public async Task<List<MediaFileInfo>> GetFilesByIdsAsync(int[] ids, MediaLoadFlags flags = MediaLoadFlags.AsNoTracking)
        {
            Guard.NotNull(ids);

            if (ids.Length == 0)
            {
                return [];
            }

            var query = _db.MediaFiles.Where(x => ids.Contains(x.Id));
            var result = await _searcher.ApplyLoadFlags(query, flags).ToListAsync();

            return result.OrderBySequence(ids).Select(ConvertMediaFile).ToList();
        }

        public async Task<AsyncOut<string>> CheckUniqueFileNameAsync(string path)
        {
            Guard.NotEmpty(path);

            // TODO: (mm) (mc) throw when path is not a file path

            if (!_helper.TokenizePath(path, false, out var pathData))
            {
                return new AsyncOut<string>(false);
            }

            string newPath = null;

            if (await CheckUniqueFileNameAsync(pathData))
            {
                newPath = pathData.FullPath;
            }

            return new AsyncOut<string>(newPath != null, newPath);
        }

        protected internal virtual async Task<bool> CheckUniqueFileNameAsync(MediaPathData pathData, CancellationToken cancelToken = default)
        {
            Guard.NotNull(pathData);

            // (perf) First make fast check. The chance that there's no dupe is much higher.
            var exists = await _db.MediaFiles.AnyAsync(x => x.Name == pathData.FileName && x.FolderId == pathData.Folder.Id, cancelToken);
            if (!exists)
            {
                return false;
            }

            var q = new MediaSearchQuery
            {
                FolderId = pathData.Folder.Id,
                // Avoid "Contains" pattern, force "StartsWith", which is faster.
                Term = pathData.FileTitle + '*',
                Deleted = null
            };

            var query = _searcher.PrepareQuery(q, MediaLoadFlags.AsNoTracking).Select(x => x.Name);
            var result = await query.ToListAsync(cancelToken);

            // Reduce by file extension in memory
            var fileNames = new HashSet<string>(
                result.Where(x => x.EndsWithNoCase('.' + pathData.Extension)),
                StringComparer.CurrentCultureIgnoreCase);

            return CheckUniqueFileName(pathData, fileNames);
        }

        protected internal virtual bool CheckUniqueFileName(MediaPathData pathData, HashSet<string> destFileNames)
        {
            Guard.NotNull(pathData);
            Guard.NotNull(destFileNames);

            if (MediaHelper.CheckUniqueFileName(pathData.FileTitle, pathData.Extension, destFileNames, out var uniqueName))
            {
                pathData.FileName = uniqueName;
                return true;
            }

            return false;
        }

        public string CombinePaths(params string[] paths)
        {
            return FolderService.NormalizePath(Path.Combine(paths), false);
        }

        public bool FindEqualFile(Stream source, IEnumerable<MediaFile> files, bool leaveOpen, out MediaFile equalFile)
        {
            Guard.NotNull(source);
            Guard.NotNull(files);

            equalFile = null;

            var bufferedSource = new BufferedReadStream(source, 8096);

            try
            {
                foreach (var file in files)
                {
                    bufferedSource.Seek(0, SeekOrigin.Begin);

                    using var other = _storageProvider.OpenRead(file);
                    if (bufferedSource.ContentsEqual(other, true))
                    {
                        equalFile = file;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (!leaveOpen)
                {
                    bufferedSource.Dispose();
                }
            }
        }

        public async Task<AsyncOut<MediaFile>> FindEqualFileAsync(Stream source, IEnumerable<MediaFile> files, bool leaveOpen)
        {
            Guard.NotNull(source);
            Guard.NotNull(files);

            var bufferedSource = new BufferedReadStream(source, 8096);

            try
            {
                foreach (var file in files)
                {
                    bufferedSource.Seek(0, SeekOrigin.Begin);

                    await using var other = await _storageProvider.OpenReadAsync(file);
                    if (await bufferedSource.ContentsEqualAsync(other, true))
                    {
                        return new AsyncOut<MediaFile>(true, file);
                    }
                }

                return new AsyncOut<MediaFile>(false);
            }
            catch
            {
                return new AsyncOut<MediaFile>(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    await bufferedSource.DisposeAsync();
                }
            }
        }

        #endregion

        #region Create/Update/Delete/Replace

        public async Task ReprocessImageAsync(MediaFileInfo fileInfo)
        {
            Guard.NotNull(fileInfo);

            var file = fileInfo.File;

            if (file.MediaType != MediaType.Image)
            {
                throw new InvalidOperationException("Only images can be reprocessed.");
            }

            var inStream = await _storageProvider.OpenReadAsync(file);

            if ((await ProcessImage(file, inStream, true)).Out(out var outImage) && (outImage is not ImageWrapper wrapper || wrapper.InStream != inStream))
            {
                // If outImage is ImageWrapper and its inStream equals the original stream,
                // then the image has not been touched.
                
                var storageItem = MediaStorageItem.FromImage(outImage);

                file.Width = outImage.Width;
                file.Height = outImage.Height;
                file.PixelSize = outImage.Width * outImage.Height;
                file.Size = (int)storageItem.SourceStream.Length;

                fileInfo.Size = new Size(outImage.Width, outImage.Height);

                // Close read stream, we gonna need it for writing now.
                inStream.Close();

                // Save/overwrite reprocessed image
                await _storageProvider.SaveAsync(file, storageItem);
                await _db.SaveChangesAsync();

                // Delete thumbnail
                await _imageCache.DeleteAsync(file);

            }
            else
            {
                // Close stream anyway
                inStream.Close();
            }
        }

        public async Task<MediaFileInfo> ReplaceFileAsync(MediaFile file, Stream inStream, string newFileName)
        {
            Guard.NotNull(file);
            Guard.NotNull(inStream);
            Guard.NotEmpty(newFileName);

            var fileInfo = ConvertMediaFile(file);
            var pathData = CreatePathData(fileInfo.Path);
            pathData.FileName = newFileName;

            var result = await ProcessFileAsync(
                file,
                pathData,
                inStream,
                isTransient: false,
                dupeFileHandling: DuplicateFileHandling.Overwrite,
                mediaValidationType: MimeValidationType.MediaTypeMustMatch);

            try
            {
                await _storageProvider.SaveAsync(result.File, result.StorageItem);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return fileInfo;
        }

        public async Task<MediaFileInfo> SaveFileAsync(
            string path,
            Stream stream,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError)
        {
            var pathData = CreatePathData(path);

            var file = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Name == pathData.FileName && x.FolderId == pathData.Folder.Id);
            var isDupe = file != null;
            var result = await ProcessFileAsync(
                file,
                pathData,
                stream ?? new MemoryStream(),
                isTransient: false,
                dupeFileHandling: dupeFileHandling);

            file = result.File;

            // We can't defer commit in this operation, MediaFile MUST be committed before saving stream.
            using var scope = new DbContextScope(_db, deferCommit: false);

            if (file.Id == 0)
            {
                _db.MediaFiles.Add(file);
            }

            try
            {
                await _db.SaveChangesAsync();
                await _storageProvider.SaveAsync(file, result.StorageItem);
            }
            catch (Exception ex)
            {
                if (!isDupe && file.Id > 0)
                {
                    // New file's metadata should be removed on storage save failure immediately
                    await DeleteFileAsync(file, true, true);
                    await _db.SaveChangesAsync();
                }

                Logger.Error(ex);
            }

            return ConvertMediaFile(file, pathData.Folder);
        }

        public async Task<IList<FileBatchResult>> BatchSaveFilesAsync(
            FileBatchSource[] sources,
            MediaFolderNode destinationFolder,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(sources);
            Guard.NotNull(destinationFolder);

            var batchResults = new List<FileBatchResult>(sources.Length);

            if (sources.Length == 0)
            {
                return batchResults;
            }

            // Use IMediaDupeDetector to get all files in destination folder for faster dupe selection.
            using var dupeDetector = _dupeDetectorFactory.GetDetector(destinationFolder.Id);

            foreach (var source in sources)
            {
                var path = CombinePaths(destinationFolder.Path, source.FileName);
                var pathData = CreatePathData(path);

                try
                {
                    var file = await dupeDetector.DetectFileAsync(pathData.FileName, cancelToken);
                    var isDupe = file != null;
                    var processFileResult = await ProcessFileAsync(
                        file,
                        pathData,
                        source.Source.SourceStream,
                        dupeDetector,
                        isTransient,
                        dupeFileHandling,
                        MimeValidationType.MediaTypeMustMatch,
                        cancelToken);

                    file = processFileResult.File;

                    batchResults.Add(new FileBatchResult
                    {
                        Source = source,
                        File = ConvertMediaFile(file, pathData.Folder),
                        StorageItem = processFileResult.StorageItem,
                        PathData = pathData,
                        IsDuplicate = isDupe,
                        UniquePath = isDupe ? pathData.FullPath : null
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    batchResults.Add(new FileBatchResult
                    {
                        Source = source,
                        PathData = pathData,
                        Exception = ex
                    });
                }
            }

            cancelToken.ThrowIfCancellationRequested();

            // 2nd pass: Commit all new MediaFile entities in one go
            foreach (var result in batchResults)
            {
                if (result.Exception == null && result.File != null && result.File.Id == 0)
                {
                    _db.MediaFiles.Add(result.File.File);
                }
            }

            // ...now commit
            using (var scope = new DbContextScope(_db, deferCommit: false))
            {
                // We can't defer commit in this operation, MediaFile MUST be committed before saving to storage.
                await _db.SaveChangesAsync(cancelToken);
            }

            cancelToken.ThrowIfCancellationRequested();

            // 3rd pass: save file stream to storage
            var hasError = false;
            foreach (var result in batchResults)
            {
                var file = result.File?.File;

                if (result.Exception != null
                    || file == null
                    || file.Id == 0
                    || result.StorageItem == null)
                {
                    continue;
                }

                try
                {
                    await _storageProvider.SaveAsync(result.File.File, result.StorageItem);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    Logger.Error(ex);
                    result.Exception = ex;

                    // New file's metadata should be removed on storage save failure
                    _db.MediaFiles.Remove(file);
                }
                finally
                {
                    // INFO: StorageItem already disposed by StorageProvider
                    result.StorageItem = null;
                }
            }

            if (hasError)
            {
                await _db.SaveChangesAsync(cancelToken);
            }

            return batchResults;
        }

        public async Task DeleteFileAsync(MediaFile file, bool permanent, bool force = false)
        {
            Guard.NotNull(file);

            if (file.Id == 0)
            {
                return;
            }

            // Delete thumb
            await _imageCache.DeleteAsync(file);

            if (!permanent)
            {
                file.Deleted = true;
                await _db.SaveChangesAsync();
            }
            else
            {
                try
                {
                    if (!force && file.Tracks.Any())
                    {
                        throw _exceptionFactory.DeleteTrackedFile(file, null);
                    }

                    // Delete BLOB
                    await _storageProvider.RemoveAsync(file);

                    // Delete media entity
                    _db.MediaFiles.Remove(file);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    if (_db.DataProvider.IsUniquenessViolationException(ex))
                    {
                        throw _exceptionFactory.DeleteTrackedFile(file, ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        protected MediaPathData CreatePathData(string path)
        {
            Guard.NotEmpty(path);

            if (!_helper.TokenizePath(path, true, out var pathData))
            {
                throw new ArgumentException(T("Admin.Media.Exception.InvalidPathExample", path), nameof(path));
            }

            if (pathData.Extension.IsEmpty())
            {
                throw new ArgumentException(T("Admin.Media.Exception.FileExtension", path), nameof(path));
            }

            return pathData;
        }

        protected async Task<(MediaStorageItem StorageItem, MediaFile File)> ProcessFileAsync(
            MediaFile file,
            MediaPathData pathData,
            Stream inStream,
            IMediaDupeDetector dupeDetector = null,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError,
            MimeValidationType mediaValidationType = MimeValidationType.MimeTypeMustMatch,
            CancellationToken cancelToken = default)
        {
            if (file != null)
            {
                var madeUniqueFileName = dupeFileHandling == DuplicateFileHandling.Overwrite
                    ? false
                    : (dupeDetector != null 
                        ? await dupeDetector.CheckUniqueFileNameAsync(pathData, cancelToken)
                        : await CheckUniqueFileNameAsync(pathData, cancelToken));

                if (dupeFileHandling == DuplicateFileHandling.ThrowError)
                {
                    var fullPath = pathData.FullPath;
                    throw _exceptionFactory.DuplicateFile(fullPath, ConvertMediaFile(file, pathData.Folder), pathData.FullPath);
                }
                else if (dupeFileHandling == DuplicateFileHandling.Rename)
                {
                    if (madeUniqueFileName)
                    {
                        file = null;
                    }
                }
            }

            if (file != null && mediaValidationType != MimeValidationType.NoValidation)
            {
                if (mediaValidationType == MimeValidationType.MimeTypeMustMatch)
                {
                    ValidateMimeTypes("Save", file.MimeType, pathData.MimeType);
                }
                else if (mediaValidationType == MimeValidationType.MediaTypeMustMatch)
                {
                    ValidateMediaTypes("Save", _typeResolver.Resolve(pathData.Extension), file.MediaType);
                }

                // Restore file if soft-deleted
                file.Deleted = false;

                // Delete thumbnail
                await _imageCache.DeleteAsync(file);
            }

            file ??= new MediaFile
            {
                IsTransient = isTransient,
                FolderId = pathData.Node.Value.Id
            };

            // Untrackable folders can never contain transient files.
            if (!pathData.Folder.CanDetectTracks)
            {
                file.IsTransient = false;
            }

            var name = pathData.FileName;
            if (name != pathData.FileName)
            {
                pathData.FileName = name;
            }

            file.Name = pathData.FileName;
            file.Extension = pathData.Extension;
            file.MimeType = pathData.MimeType;
            file.MediaType ??= _typeResolver.Resolve(pathData.Extension, pathData.MimeType);

            // Process image
            if (inStream != null && inStream.Length > 0 && file.MediaType == MediaType.Image && (await ProcessImage(file, inStream)).Out(out var outImage))
            {
                var storageItem = MediaStorageItem.FromImage(outImage);

                file.Width = outImage.Width;
                file.Height = outImage.Height;
                file.PixelSize = outImage.Width * outImage.Height;
                file.Size = (int)storageItem.SourceStream.Length;

                return (storageItem, file);
            }
            else
            {
                file.RefreshMetadata(inStream, _imageProcessor.Factory);

                return (MediaStorageItem.FromStream(inStream), file);
            }
        }

        protected async Task<AsyncOut<IImage>> ProcessImage(MediaFile file, Stream inStream, bool isReprocess = false)
        {
            // Determine image format
            var format = _imageProcessor.Factory.FindFormatByExtension(file.Extension) ?? new UnsupportedImageFormat(file.MimeType, file.Extension);
            
            // Try to read original dimensions from binary header
            var originalSize = CommonHelper.TryAction(() => ImageHeader.GetPixelSize(inStream, file.MimeType));

            var info = new GenericImageInfo(originalSize, format);

            IImage outImage;

            if (info.Format is UnsupportedImageFormat)
            {
                outImage = new ImageWrapper(inStream, info);
                return new AsyncOut<IImage>(true, outImage);
            }

            var maxSize = _mediaSettings.MaximumImageSize;

            var query = new ProcessImageQuery(inStream)
            {
                Format = file.Extension,
                DisposeSource = true,
                ExecutePostProcessor = ImagePostProcessingEnabled,
                IsValidationMode = true,
                Notify = !isReprocess
            };

            if (!isReprocess)
            {
                // If force is true, we want it to be processed even if image is smaller than max allowed size
                if (originalSize.IsEmpty || (originalSize.Height <= maxSize && originalSize.Width <= maxSize))
                {
                    // Give subscribers the chance to (pre)-process
                    var evt = new ImageUploadedEvent(query, info);
                    await _eventPublisher.PublishAsync(evt);
                    outImage = evt.ResultImage ?? new ImageWrapper(inStream, info);

                    return new AsyncOut<IImage>(true, outImage);
                }
            }

            query.MaxSize = maxSize;

            using var result = await _imageProcessor.ProcessImageAsync(query, false);
            outImage = result.Image;

            return new AsyncOut<IImage>(true, outImage);
        }

        #endregion

        #region Copy & Move

        public async Task<FileOperationResult> CopyFileAsync(MediaFileInfo mediaFile, string destinationFileName, DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError)
        {
            Guard.NotNull(mediaFile);
            Guard.NotEmpty(destinationFileName);

            var destPathData = CreateDestinationPathData(mediaFile.File, destinationFileName);
            var destFileName = destPathData.FileName;
            var destFolderId = destPathData.Folder.Id;

            var dupe = mediaFile.FolderId == destPathData.Folder.Id
                // Source folder equals dest folder, so same file
                ? mediaFile.File
                // Another dest folder, check for duplicate by file name
                : await _db.MediaFiles.FirstOrDefaultAsync(x => x.Name == destFileName && x.FolderId == destFolderId);

            var copyResult = await InternalCopyFile(
                mediaFile.File,
                destPathData,
                true /* copyData */,
                (DuplicateEntryHandling)((int)dupeFileHandling),
                () => Task.FromResult(dupe),
                p => CheckUniqueFileNameAsync(p));

            return new FileOperationResult
            {
                Operation = "copy",
                DuplicateFileHandling = dupeFileHandling,
                SourceFile = mediaFile,
                DestinationFile = ConvertMediaFile(copyResult.Copy, destPathData.Folder),
                IsDuplicate = copyResult.IsDupe,
                UniquePath = copyResult.IsDupe ? destPathData.FullPath : null
            };
        }

        private async Task<(MediaFile Copy, bool IsDupe)> InternalCopyFile(
            MediaFile file,
            MediaPathData destPathData,
            bool copyData,
            DuplicateEntryHandling dupeEntryHandling,
            Func<Task<MediaFile>> dupeFileSelector,
            Func<MediaPathData, Task> uniqueFileNameChecker)
        {
            // Find dupe and handle
            var isDupe = false;

            var dupe = await dupeFileSelector();
            if (dupe != null)
            {
                switch (dupeEntryHandling)
                {
                    case DuplicateEntryHandling.Skip:
                        await uniqueFileNameChecker(destPathData);
                        return (dupe, true);
                    case DuplicateEntryHandling.ThrowError:
                        var fullPath = destPathData.FullPath;
                        await uniqueFileNameChecker(destPathData);
                        throw _exceptionFactory.DuplicateFile(fullPath, ConvertMediaFile(dupe), destPathData.FullPath);
                    case DuplicateEntryHandling.Rename:
                        await uniqueFileNameChecker(destPathData);
                        dupe = null;
                        break;
                    case DuplicateEntryHandling.Overwrite:
                        if (file.FolderId == destPathData.Folder.Id)
                        {
                            throw new IOException(T("Admin.Media.Exception.Overwrite"));
                        }
                        break;
                }
            }

            isDupe = dupe != null;
            var copy = dupe ?? new MediaFile();

            // Simple clone
            MapMediaFile(file, copy);

            // Set folder id
            copy.FolderId = destPathData.Folder.Id;

            // A copied file cannot stay in deleted state
            copy.Deleted = false;

            // Set name stuff
            if (!copy.Name.EqualsNoCase(destPathData.FileName))
            {
                copy.Name = destPathData.FileName;
                copy.Extension = destPathData.Extension;
                copy.MimeType = destPathData.MimeType;
            }

            // Save to DB
            if (isDupe)
            {
                _db.TryUpdate(copy);
            }
            else
            {
                _db.MediaFiles.Add(copy);
            }

            await _db.SaveChangesAsync();

            // Copy data: blob, alt, title etc.
            if (copyData)
            {
                await InternalCopyFileData(file, copy);
            }

            return (copy, isDupe);
        }

        private async Task InternalCopyFileData(MediaFile file, MediaFile copy)
        {
            await _storageProvider.SaveAsync(copy, MediaStorageItem.FromStream(await _storageProvider.OpenReadAsync(file)));
            await _imageCache.DeleteAsync(copy);

            // Tags.
            await _db.LoadCollectionAsync(file, (MediaFile x) => x.Tags);

            var existingTagsIds = copy.Tags.Select(x => x.Id).ToList();

            foreach (var tag in file.Tags)
            {
                if (!existingTagsIds.Contains(tag.Id))
                {
                    copy.Tags.Add(tag);
                    existingTagsIds.Add(tag.Id);
                }
            }

            // Localized values.
            var languages = _languageService.GetAllLanguages(true);

            foreach (var language in languages)
            {
                var title = file.GetLocalized(x => x.Title, language.Id, false, false).Value;
                if (title.HasValue())
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(copy, x => x.Title, title, language.Id);
                }

                var alt = file.GetLocalized(x => x.Alt, language.Id, false, false).Value;
                if (alt.HasValue())
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(copy, x => x.Alt, alt, language.Id);
                }
            }

            await _db.SaveChangesAsync();
            _db.DetachEntities<MediaTag>();
        }

        public async Task<MediaFileInfo> MoveFileAsync(MediaFile file, string destinationFileName, DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError)
        {
            var moveOperation = await ValidateMoveOperation(file, destinationFileName, dupeFileHandling);
            var destPathData = moveOperation.DestPathData;
            var nameChanged = moveOperation.NameChanged;

            if (moveOperation.Valid)
            {
                file.FolderId = destPathData.Folder.Id;

                // A moved file cannot stay in deleted state
                file.Deleted = false;

                if (nameChanged)
                {
                    var title = destPathData.FileTitle;
                    var ext = destPathData.Extension.ToLower();

                    file.Name = title + "." + ext;

                    if (ext != file.Extension.ToLower())
                    {
                        await _storageProvider.ChangeExtensionAsync(file, ext);
                        file.Extension = ext;
                    }

                    await _imageCache.DeleteAsync(file);
                }

                _db.TryUpdate(file);
                await _db.SaveChangesAsync();
            }

            return ConvertMediaFile(file, destPathData.Folder);
        }

        private async Task<(bool Valid, bool NameChanged, MediaPathData DestPathData)> ValidateMoveOperation(
            MediaFile file,
            string destinationFileName,
            DuplicateFileHandling dupeFileHandling)
        {
            Guard.NotNull(file);
            Guard.NotEmpty(destinationFileName);

            var destPathData = CreateDestinationPathData(file, destinationFileName);
            var destFileName = destPathData.FileName;
            var destFolderId = destPathData.Folder.Id;
            var folderChanged = destFolderId != file.FolderId;
            var shouldRestore = false;

            var nameChanged = !destFileName.EqualsNoCase(file.Name);

            if (file.FolderId.HasValue && folderChanged)
            {
                // When "Move" operation: ensure file stays in source album.
                ValidateAlbums("Move", file.FolderId.Value, destFolderId);
            }

            if (nameChanged)
            {
                // Ensure both MIME types are equal
                ValidateMimeTypes("Move", file.MimeType, destPathData.MimeType);
            }

            // Check whether destination file exists
            if (!folderChanged && file.Deleted)
            {
                // Special case where a file is moved from trash to its origin location.
                // In this case the file should just be restored without any dupe check.
                shouldRestore = true;
            }
            else
            {
                var dupe = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Name == destFileName && x.FolderId == destFolderId);
                if (dupe != null)
                {
                    if (!folderChanged)
                    {
                        throw _exceptionFactory.IdenticalPaths(ConvertMediaFile(file, destPathData.Folder));
                    }

                    switch (dupeFileHandling)
                    {
                        case DuplicateFileHandling.ThrowError:
                            var fullPath = destPathData.FullPath;
                            MediaHelper.CheckUniqueFileName(destPathData.FileTitle, destPathData.Extension, dupe.Name, out _);
                            throw _exceptionFactory.DuplicateFile(fullPath, ConvertMediaFile(dupe, destPathData.Folder), destPathData.FullPath);
                        case DuplicateFileHandling.Rename:
                            if (MediaHelper.CheckUniqueFileName(destPathData.FileTitle, destPathData.Extension, dupe.Name, out var uniqueName))
                            {
                                nameChanged = true;
                                destPathData.FileName = uniqueName;
                                return (true, nameChanged, destPathData);
                            }
                            break;
                        case DuplicateFileHandling.Overwrite:
                            await DeleteFileAsync(dupe, true);
                            break;
                    }
                }
            }

            return (folderChanged || nameChanged || shouldRestore, nameChanged, destPathData);
        }

        #endregion

        #region URL generation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetUrl(MediaFileInfo file, ProcessImageQuery imageQuery, string host = null, bool doFallback = true)
        {
            return _urlGenerator.GenerateUrl(file, imageQuery, host, doFallback);
        }

        #endregion

        #region Utils

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MediaFileInfo ConvertMediaFile(MediaFile file)
        {
            return ConvertMediaFile(file, _folderService.FindNode(file)?.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected MediaFileInfo ConvertMediaFile(MediaFile file, MediaFolderNode folder)
        {
            var mediaFile = new MediaFileInfo(file, this, _urlGenerator, folder?.Path)
            {
                ThumbSize = _mediaSettings.ProductThumbPictureSize
            };

            return mediaFile;
        }

        public async Task<int> EnsureMetadataResolvedAsync(string folderPath = null)
        {
            if (_storageProvider is not FileSystemMediaStorageProvider storageProvider)
            {
                throw new InvalidOperationException($"Retrospective resolution of media metadata is only possible if {nameof(FileSystemMediaStorageProvider)} is active.");
            }
            
            var message = string.Empty;

            var query = _db.MediaFiles
                .Where(x => string.IsNullOrEmpty(x.Name));

            if (folderPath.HasValue())
            {
                var node = _folderService.GetNodeByPath(folderPath) ?? throw new ArgumentException($"The folder path '{folderPath}' does not exist.");
                query = query.Where(x => x.FolderId == node.Value.Id);
            }

            var fastPager = new FastPager<MediaFile>(query);
            var numResolved = 0;

            while ((await fastPager.ReadNextPageAsync<MediaFile>()).Out(out var files))
            {
                foreach (var file in files)
                {
                    var filePath = storageProvider.GetPath(file);

                    _helper.TokenizePath(filePath, true, out var pathData);
                    
                    file.Name = pathData.FileName;
                    file.Extension = pathData.Extension;
                    file.MimeType = pathData.MimeType;
                    file.MediaType ??= _typeResolver.Resolve(pathData.Extension, pathData.MimeType);

                    await EnsureMetadataResolvedAsync(file, false);
                }

                numResolved += await _db.SaveChangesAsync();
            }

            return numResolved;
        }

        private async Task EnsureMetadataResolvedAsync(MediaFile file, bool saveOnResolve)
        {
            var mediaType = _typeResolver.Resolve(file.Extension, file.MimeType);

            var resolveDimensions = mediaType == MediaType.Image && (file.Width == null || file.Height == null);
            var resolveSize = file.Size <= 0;

            Stream stream = null;

            if (resolveDimensions || resolveSize)
            {
                stream = await _storageProvider.OpenReadAsync(file);
            }

            // Resolve image dimensions
            if (stream != null)
            {
                try
                {
                    if (resolveSize)
                    {
                        file.Size = (int)stream.Length;
                    }

                    if (resolveDimensions)
                    {
                        var size = ImageHeader.GetPixelSize(stream, file.MimeType, true);
                        file.Width = size.Width;
                        file.Height = size.Height;
                        file.PixelSize = size.Width * size.Height;
                    }

                    if (saveOnResolve && (resolveSize || resolveDimensions))
                    {
                        try
                        {
                            _db.TryUpdate(file);
                            await _db.SaveChangesAsync();
                        }
                        catch (InvalidOperationException ioe)
                        {
                            // Ignore exception for pictures that already have been processed.
                            if (!ioe.IsAlreadyAttachedEntityException())
                            {
                                throw;
                            }
                        }
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }

        private static void MapMediaFile(MediaFile from, MediaFile to)
        {
            to.Alt = from.Alt;
            to.Deleted = from.Deleted;
            to.Extension = from.Extension;
            to.FolderId = from.FolderId;
            to.Height = from.Height;
            to.Hidden = from.Hidden;
            to.IsTransient = from.IsTransient; // TBD: (mm) really?
            to.MediaType = from.MediaType;
            to.Metadata = from.Metadata;
            to.MimeType = from.MimeType;
            to.Name = from.Name;
            to.PixelSize = from.PixelSize;
            to.Size = from.Size;
            to.Title = from.Title;
            to.Version = from.Version;
            to.Width = from.Width;
        }

        private MediaPathData CreateDestinationPathData(MediaFile file, string destinationFileName)
        {
            if (!_helper.TokenizePath(destinationFileName, true, out var pathData))
            {
                // Passed path is NOT a path, but a file name

                if (IsPath(destinationFileName))
                {
                    // ...but file name includes path chars, which is not allowed
                    throw new ArgumentException(
                        T("Admin.Media.Exception.InvalidPath", Path.GetDirectoryName(destinationFileName)),
                        nameof(destinationFileName));
                }

                if (file.FolderId == null)
                {
                    throw new NotSupportedException(T("Admin.Media.Exception.FolderAssignment"));
                }

                pathData = new MediaPathData(_folderService.GetNodeById(file.FolderId.Value), destinationFileName);
            }

            return pathData;
        }

        private void ValidateMimeTypes(string operation, string mime1, string mime2)
        {
            if (!mime1.Equals(mime2, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: (mm) Create this and all other generic exceptions by MediaExceptionFactory
                throw new NotSupportedException(T("Admin.Media.Exception.MimeType", operation, mime1, mime2));
            }
        }

        private void ValidateMediaTypes(string operation, string mime1, string mime2)
        {
            if (mime1 != mime2)
            {
                // TODO: (mm) Create this and all other generic exceptions by MediaExceptionFactory
                throw new NotSupportedException(T("Admin.Media.Exception.MediaType", operation, mime1, mime2));
            }
        }

        private void ValidateAlbums(string operation, int folderId1, int folderId2)
        {
            if (!_folderService.AreInSameAlbum(folderId1, folderId2))
            {
                throw _exceptionFactory.NotSameAlbum(
                    _folderService.GetNodeById(folderId1).Value.Path,
                    _folderService.GetNodeById(folderId2).Value.Path);
            }
        }

        private static readonly SearchValues<char> _pathSeparators = SearchValues.Create("/\\");
        private static bool IsPath(string path)
        {
            return path.AsSpan().IndexOfAny(_pathSeparators) > -1;
        }

        #endregion
    }
}
