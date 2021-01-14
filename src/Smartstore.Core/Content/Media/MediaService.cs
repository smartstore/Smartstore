using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Events;
using Smartstore.Imaging;
using Smartstore.Threading;

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
            Guard.NotNull(query, nameof(query));

            var q = _searcher.PrepareQuery(query, MediaLoadFlags.None);
            return await q.CountAsync();
        }

        public async Task<FileCountResult> CountFilesGroupedAsync(MediaFilesFilter filter)
        {
            Guard.NotNull(filter, nameof(filter));

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
                                select new FileCountResult
                                {
                                    Total = g.Count(),
                                    Trash = g.Count(x => x.Deleted),
                                    Unassigned = g.Count(x => !x.Deleted && x.FolderId == null),
                                    Transient = g.Count(x => !x.Deleted && x.IsTransient == true),
                                    Orphan = g.Count(x => !x.Deleted && x.FolderId > 0 && !untrackableFolderIds.Contains(x.FolderId.Value) && !x.Tracks.Any())
                                }).FirstOrDefaultAsync() ?? new FileCountResult();

            if (result.Total == 0)
            {
                result.Folders = new Dictionary<int, int>();
                return result;
            }

            // Determine file count for each folder
            var byFolders = from f in q
                            where f.FolderId > 0 && !f.Deleted
                            group f by f.FolderId.Value into grp
                            select grp;

            result.Folders = await byFolders
                .Select(grp => new { FolderId = grp.Key, Count = grp.Count() })
                .ToDictionaryAsync(k => k.FolderId, v => v.Count);

            result.Filter = filter;

            return result;
        }

        public async Task<MediaSearchResult> SearchFilesAsync(
            MediaSearchQuery query,
            Func<IQueryable<MediaFile>, IQueryable<MediaFile>> queryModifier,
            MediaLoadFlags flags = MediaLoadFlags.AsNoTracking)
        {
            Guard.NotNull(query, nameof(query));

            var files = _searcher.SearchFiles(query, flags);
            if (queryModifier != null)
            {
                files.AlterQuery(queryModifier);
            }

            return new MediaSearchResult(await files.LoadAsync(), ConvertMediaFile);
        }

        #endregion

        #region Read

        public async Task<bool> FileExistsAsync(string path)
        {
            Guard.NotEmpty(path, nameof(path));

            if (_helper.TokenizePath(path, false, out var tokens))
            {
                return await _db.MediaFiles.AnyAsync(x => x.FolderId == tokens.Folder.Id && x.Name == tokens.FileName);
            }

            return false;
        }

        public async Task<MediaFileInfo> GetFileByPathAsync(string path, MediaLoadFlags flags = MediaLoadFlags.None)
        {
            Guard.NotEmpty(path, nameof(path));

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
            Guard.IsPositive(folderId, nameof(folderId));
            Guard.NotEmpty(fileName, nameof(fileName));

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
            Guard.NotNull(ids, nameof(ids));

            var query = _db.MediaFiles.Where(x => ids.Contains(x.Id));
            var result = await _searcher.ApplyLoadFlags(query, flags).ToListAsync();

            return result.OrderBySequence(ids).Select(ConvertMediaFile).ToList();
        }

        public async Task<AsyncOut<string>> CheckUniqueFileNameAsync(string path)
        {
            Guard.NotEmpty(path, nameof(path));

            // TODO: (mm) (mc) throw when path is not a file path

            if (!_helper.TokenizePath(path, false, out var pathData))
            {
                return new AsyncOut<string>(false);
            }

            string newPath = null;

            if (await CheckUniqueFileName(pathData))
            {
                newPath = pathData.FullPath;
            }

            return new AsyncOut<string>(newPath != null, newPath);
        }

        protected internal virtual async Task<bool> CheckUniqueFileName(MediaPathData pathData)
        {
            // (perf) First make fast check
            var exists = await _db.MediaFiles.AnyAsync(x => x.Name == pathData.FileName && x.FolderId == pathData.Folder.Id);
            if (!exists)
            {
                return false;
            }

            var q = new MediaSearchQuery
            {
                FolderId = pathData.Folder.Id,
                Term = string.Concat(pathData.FileTitle, "*.", pathData.Extension),
                Deleted = null
            };

            var query = _searcher.PrepareQuery(q, MediaLoadFlags.AsNoTracking).Select(x => x.Name);
            var files = new HashSet<string>(await query.ToListAsync(), StringComparer.CurrentCultureIgnoreCase);

            if (_helper.CheckUniqueFileName(pathData.FileTitle, pathData.Extension, files, out var uniqueName))
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
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(files, nameof(files));

            equalFile = null;

            try
            {
                foreach (var file in files)
                {
                    source.Seek(0, SeekOrigin.Begin);

                    using (var other = _storageProvider.OpenRead(file))
                    {
                        if (source.ContentsEqual(other, true))
                        {
                            equalFile = file;
                            return true;
                        }
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
                    source.Dispose();
                }
            }
        }

        public async Task<AsyncOut<MediaFile>> FindEqualFileAsync(Stream source, IEnumerable<MediaFile> files, bool leaveOpen)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(files, nameof(files));

            try
            {
                foreach (var file in files)
                {
                    source.Seek(0, SeekOrigin.Begin);

                    await using (var other = await _storageProvider.OpenReadAsync(file))
                    {
                        if (await source.ContentsEqualAsync(other, true))
                        {
                            return new AsyncOut<MediaFile>(true, file);
                        }
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
                    await source.DisposeAsync();
                }
            }
        }

        #endregion

        #region Create/Update/Delete/Replace

        public async Task<MediaFileInfo> ReplaceFileAsync(MediaFile file, Stream inStream, string newFileName)
        {
            Guard.NotNull(file, nameof(file));
            Guard.NotNull(inStream, nameof(inStream));
            Guard.NotEmpty(newFileName, nameof(newFileName));

            var fileInfo = ConvertMediaFile(file);
            var pathData = CreatePathData(fileInfo.Path);
            pathData.FileName = newFileName;

            var result = await ProcessFile(file, pathData, inStream, false, DuplicateFileHandling.Overwrite, MimeValidationType.MediaTypeMustMatch);

            try
            {
                await _storageProvider.SaveAsync(result.File, result.StorageItem);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return fileInfo;
        }

        public async Task<MediaFileInfo> SaveFileAsync(string path, Stream stream, bool isTransient = true, DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError)
        {
            var pathData = CreatePathData(path);

            var file = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Name == pathData.FileName && x.FolderId == pathData.Folder.Id);
            var isNewFile = file == null;
            var result = await ProcessFile(file, pathData, stream, isTransient, dupeFileHandling);
            file = result.File;

            if (file.Id == 0)
            {
                _db.MediaFiles.Add(file);
                await _db.SaveChangesAsync();
            }

            try
            {
                await _storageProvider.SaveAsync(file, result.StorageItem);
            }
            catch (Exception ex)
            {
                if (isNewFile)
                {
                    // New file's metadata should be removed on storage save failure immediately
                    await DeleteFileAsync(file, true, true);
                    await _db.SaveChangesAsync();
                }

                Logger.Error(ex);
            }

            return ConvertMediaFile(file, pathData.Folder);
        }

        public async Task DeleteFileAsync(MediaFile file, bool permanent, bool force = false)
        {
            Guard.NotNull(file, nameof(file));

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

                    // Delete entity
                    _db.MediaFiles.Remove(file);
                    await _db.SaveChangesAsync();

                    // Delete from storage
                    await _storageProvider.RemoveAsync(file);
                }
                catch (DbUpdateException ex)
                {
                    if (ex.IsUniquenessViolationException())
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
            Guard.NotEmpty(path, nameof(path));

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

        protected async Task<(MediaStorageItem StorageItem, MediaFile File)> ProcessFile(
            MediaFile file,
            MediaPathData pathData,
            Stream inStream,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError,
            MimeValidationType mediaValidationType = MimeValidationType.MimeTypeMustMatch)
        {
            if (file != null)
            {
                if (dupeFileHandling == DuplicateFileHandling.ThrowError)
                {
                    var fullPath = pathData.FullPath;
                    await CheckUniqueFileName(pathData);
                    throw _exceptionFactory.DuplicateFile(fullPath, ConvertMediaFile(file, pathData.Folder), pathData.FullPath);
                }
                else if (dupeFileHandling == DuplicateFileHandling.Rename)
                {
                    if (await CheckUniqueFileName(pathData))
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
            if (file.MediaType == null)
            {
                file.MediaType = _typeResolver.Resolve(pathData.Extension, pathData.MimeType);
            }

            // Process image
            if (inStream != null && inStream.Length > 0 && file.MediaType == MediaType.Image && (await ProcessImage(file, inStream)).Out(out var outImage))
            {
                file.Width = outImage.Width;
                file.Height = outImage.Height;
                file.PixelSize = outImage.Width * outImage.Height;

                return (MediaStorageItem.FromImage(outImage), file);
            }
            else
            {
                file.RefreshMetadata(inStream, _imageProcessor.Factory);

                return (MediaStorageItem.FromStream(inStream), file);
            }
        }

        protected async Task<AsyncOut<IImage>> ProcessImage(MediaFile file, Stream inStream)
        {
            var originalSize = Size.Empty;
            var format = _imageProcessor.Factory.FindFormatByExtension(file.Extension) ?? new UnsupportedImageFormat(file.MimeType, file.Extension);

            try
            {
                originalSize = ImageHeader.GetPixelSize(inStream, file.MimeType);
            }
            catch 
            { 
            }

            IImage outImage;

            if (format is UnsupportedImageFormat)
            {
                outImage = new ImageWrapper(inStream, originalSize, format);
                return new AsyncOut<IImage>(true, outImage);
            }

            var maxSize = _mediaSettings.MaximumImageSize;

            var query = new ProcessImageQuery(inStream)
            {
                Format = file.Extension,
                DisposeSource = true,
                ExecutePostProcessor = ImagePostProcessingEnabled,
                IsValidationMode = true
            };

            if (originalSize.IsEmpty || (originalSize.Height <= maxSize && originalSize.Width <= maxSize))
            {
                // Give subscribers the chance to (pre)-process
                var evt = new ImageUploadedEvent(query, originalSize);
                await _eventPublisher.PublishAsync(evt);
                outImage = evt.ResultImage ?? new ImageWrapper(inStream, originalSize, format);

                return new AsyncOut<IImage>(true, outImage);
            }

            query.MaxSize = maxSize;

            using (var result = await _imageProcessor.ProcessImageAsync(query, false))
            {
                outImage = result.Image;
                return new AsyncOut<IImage>(true, outImage);
            }
        }

        #endregion

        #region Copy & Move

        public async Task<FileOperationResult> CopyFileAsync(MediaFileInfo mediaFile, string destinationFileName, DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));
            Guard.NotEmpty(destinationFileName, nameof(destinationFileName));

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
                p => CheckUniqueFileName(p));

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
                        await uniqueFileNameChecker (destPathData);
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
                _db.TryChangeState(copy, EntityState.Modified);
            }
            else
            {
                _db.MediaFiles.Add(copy);
            }

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

                _db.TryChangeState(file, EntityState.Modified);
                await _db.SaveChangesAsync();
            }

            return ConvertMediaFile(file, destPathData.Folder);
        }

        private async Task<(bool Valid, bool NameChanged, MediaPathData DestPathData)> ValidateMoveOperation(
            MediaFile file,
            string destinationFileName,
            DuplicateFileHandling dupeFileHandling)
        {
            Guard.NotNull(file, nameof(file));
            Guard.NotEmpty(destinationFileName, nameof(destinationFileName));

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
                            _helper.CheckUniqueFileName(destPathData.FileTitle, destPathData.Extension, dupe.Name, out _);
                            throw _exceptionFactory.DuplicateFile(fullPath, ConvertMediaFile(dupe, destPathData.Folder), destPathData.FullPath);
                        case DuplicateFileHandling.Rename:
                            if (_helper.CheckUniqueFileName(destPathData.FileTitle, destPathData.Extension, dupe.Name, out var uniqueName))
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
            var mediaFile = new MediaFileInfo(file, _storageProvider, _urlGenerator, folder?.Path)
            {
                ThumbSize = _mediaSettings.ProductThumbPictureSize
            };

            return mediaFile;
        }

        private async Task EnsureMetadataResolvedAsync(MediaFile file, bool saveOnResolve)
        {
            var mediaType = _typeResolver.Resolve(file.Extension, file.MimeType);

            var resolveDimensions = mediaType == MediaType.Image && file.Width == null && file.Height == null;
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
                            _db.TryChangeState(file, EntityState.Modified);
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

        private static bool IsPath(string path)
        {
            return path.IndexOfAny(new[] { '/', '\\' }) > -1;
        }

        #endregion
    }
}
