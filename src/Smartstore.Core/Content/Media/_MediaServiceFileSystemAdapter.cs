using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;
using Smartstore.Collections;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore;
using Dasync.Collections;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// A media service implementation that emulates file system.
    /// </summary>
    public partial class MediaServiceFileSystemAdapter : FileSystemBase, IMediaFileSystem
    {
        private readonly IMediaService _mediaService;
        private readonly IMediaSearcher _mediaSearcher;
        private readonly IFolderService _folderService;
        private readonly MediaHelper _mediaHelper;
        private readonly IMediaStorageProvider _storageProvider;
        private readonly MediaExceptionFactory _exceptionFactory;
        private readonly string _mediaRootPath;

        public MediaServiceFileSystemAdapter(
            IMediaService mediaService,
            IMediaSearcher mediaSearcher,
            IFolderService folderService,
            IMediaStorageConfiguration mediaStorageConfiguration,
            MediaHelper mediaHelper,
            MediaExceptionFactory exceptionFactory)
        {
            _mediaService = mediaService;
            _mediaSearcher = mediaSearcher;
            _folderService = folderService;
            _mediaHelper = mediaHelper;
            _storageProvider = mediaService.StorageProvider;
            _exceptionFactory = exceptionFactory;
            _mediaRootPath = mediaStorageConfiguration.PublicPath;
        }

        protected string Fix(string path)
            => path.Replace('\\', '/');

        #region IMediaFileSystem

        public bool IsCloudStorage => _mediaService.StorageProvider.IsCloudStorage;

        public string PublicPath => throw new NotImplementedException();

        public string StoragePath => throw new NotImplementedException();

        public string MapToPublicUrl(IFile file, bool forCloud = false)
        {
            throw new NotImplementedException();
        }

        public string MapToPublicUrl(string path, bool forCloud = false)
        {
            throw new NotImplementedException();
        }

        public string MapUrlToStoragePath(string url)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IFileProvider

        public override IFileInfo GetFileInfo(string subpath)
            => GetFile(subpath);

        public override IDirectoryContents GetDirectoryContents(string subpath)
            => throw new NotSupportedException();

        public override IChangeToken Watch(string filter)
            => throw new NotSupportedException();

        #endregion

        #region IFileSystem

        public override string Root => string.Empty;

        public override bool CheckUniqueFileName(string subpath, out string newPath)
        {
            if (CheckUniqueFileNameAsync(subpath).Await().Out(out newPath))
            {
                return true;
            }

            return false;
        }

        public override Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath)
        {
            return _mediaService.CheckUniqueFileNameAsync(subpath);
        }

        public override void CopyFile(string subpath, string newPath, bool overwrite = false)
        {
            CopyFileAsync(subpath, newPath, overwrite).Await();
        }

        public override async Task CopyFileAsync(string subpath, string newPath, bool overwrite = false)
        {
            Guard.NotEmpty(newPath, nameof(newPath));

            var sourceFile = await _mediaService.GetFileByPathAsync(subpath);
            if (sourceFile == null)
            {
                throw _exceptionFactory.FileNotFound(subpath);
            }

            await _mediaService.CopyFileAsync(
                sourceFile,
                newPath,
                overwrite ? DuplicateFileHandling.Overwrite : DuplicateFileHandling.ThrowError);
        }

        public override long CountFiles(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return CountFilesAsync(subpath, pattern, predicate, deep).Await();
        }

        public override async Task<long> CountFilesAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            if (predicate == null)
            {
                var node = _folderService.GetNodeByPath(subpath);
                if (node == null)
                {
                    throw _exceptionFactory.FolderNotFound(subpath);
                }

                var query = new MediaSearchQuery
                {
                    FolderId = node.Value.Id,
                    DeepSearch = deep,
                    Term = pattern
                };

                return await _mediaService.CountFilesAsync(query);
            }

            var files = this.EnumerateFilesAsync(subpath, pattern, deep);
            throw new NotImplementedException(); // TODO: (core) (mm) CountFilesAsync
        }

        public override IFile CreateFile(string subpath, Stream inStream = null, bool overwrite = false)
        {
            return CreateFileAsync(subpath, inStream, overwrite).Await();
        }

        public override async Task<IFile> CreateFileAsync(string subpath, Stream inStream = null, bool overwrite = false)
        {
            return await _mediaService.SaveFileAsync(subpath, inStream, false, overwrite ? DuplicateFileHandling.Overwrite : DuplicateFileHandling.ThrowError);
        }

        public override bool DirectoryExists(string subpath)
        {
            return _mediaService.FolderExists(subpath);
        }

        public override IEnumerable<IFileEntry> EnumerateEntries(string subpath = null, string pattern = "*", bool deep = false)
        {
            throw new NotImplementedException();
        }

        public override IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string subpath = null, string pattern = "*", bool deep = false)
        {
            var node = _folderService.GetNodeByPath(subpath);
            if (node == null)
            {
                throw _exceptionFactory.FolderNotFound(subpath);
            }

            // TODO: (core) Apply pattern parameter to folder enumeration
            var folders = deep ? node.FlattenNodes(false) : node.Children;
            var result = folders.Select(x => new MediaFolderInfo(x))
                .Cast<IFileEntry>()
                .ToAsyncEnumerable();

            var query = new MediaSearchQuery
            {
                FolderId = node.Value.Id,
                DeepSearch = deep,
                Term = pattern
            };

            var files = _mediaSearcher.SearchFiles(query).SourceQuery
                .AsAsyncEnumerable()
                .Select(_mediaService.ConvertMediaFile);

            result = result.Concat(files);

            return result;
        }

        public override bool FileExists(string subpath)
        {
            return _mediaService.FileExistsAsync(subpath).Await();
        }

        public override Task<bool> FileExistsAsync(string subpath)
        {
            return _mediaService.FileExistsAsync(subpath);
        }

        public override IDirectory GetDirectory(string subpath)
        {
            var node = _folderService.GetNodeByPath(subpath);
            if (node == null)
            {
                node = new TreeNode<MediaFolderNode>(new MediaFolderNode { Path = subpath, Name = Path.GetFileName(Fix(subpath)) });
            }

            return new MediaFolderInfo(node);
        }

        public override long GetDirectorySize(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            throw new NotImplementedException();
        }

        public override Task<long> GetDirectorySizeAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            throw new NotImplementedException();
        }

        public override IFile GetFile(string subpath)
        {
            return GetFileAsync(subpath).Await();
        }

        public override async Task<IFile> GetFileAsync(string subpath)
        {
            var file = await _mediaService.GetFileByPathAsync(subpath, MediaLoadFlags.AsNoTracking);
            if (file == null)
            {
                var mediaFile = new MediaFile
                {
                    Name = Path.GetFileName(subpath),
                    Extension = Path.GetExtension(subpath).TrimStart('.'),
                    MimeType = MimeTypes.MapNameToMimeType(subpath),
                    FolderId = _folderService.GetNodeByPath(Fix(Path.GetDirectoryName(subpath)))?.Value.Id
                };

                file = _mediaService.ConvertMediaFile(mediaFile);
            }

            return file;
        }

        public override string MapPath(string subpath)
            => throw new NotSupportedException();

        public override void MoveEntry(IFileEntry entry, string newPath)
        {
            MoveEntryAsync(entry, newPath).Await();
        }

        public override Task MoveEntryAsync(IFileEntry entry, string newPath)
        {
            Guard.NotNull(entry, nameof(entry));
            Guard.NotEmpty(newPath, nameof(newPath));

            if (entry is MediaFile file)
            {
                return _mediaService.MoveFileAsync(file, newPath);
            }
            else if (entry is IDirectory dir)
            {
                return _mediaService.MoveFolderAsync(dir.SubPath, newPath);
            }

            return Task.CompletedTask;
        }

        public override string PathCombine(params string[] paths)
        {
            return _mediaService.CombinePaths(paths);
        }

        public override bool TryCreateDirectory(string subpath)
        {
            return TryCreateDirectoryAsync(subpath).Await();
        }

        public override async Task<bool> TryCreateDirectoryAsync(string subpath)
        {
            if (await _mediaService.FileExistsAsync(subpath))
            {
                throw new FileSystemException($"Cannot create directory because the path '{subpath}' already exists and is a file.");
            }

            try
            {
                return await _mediaService.CreateFolderAsync(subpath) != null;
            }
            catch
            {
                return false;
            }
        }

        public override bool TryDeleteDirectory(string subpath)
        {
            return TryDeleteDirectoryAsync(subpath).Await();
        }

        public override async Task<bool> TryDeleteDirectoryAsync(string subpath)
        {
            try
            {
                var result = await _mediaService.DeleteFolderAsync(subpath, FileHandling.Delete);
                return result.DeletedFolderIds.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public override bool TryDeleteFile(string subpath)
        {
            return TryDeleteFileAsync(subpath).Await();
        }

        public override async Task<bool> TryDeleteFileAsync(string subpath)
        {
            var file = await _mediaService.GetFileByPathAsync(subpath);
            if (file == null)
            {
                return false;
            }

            try
            {
                await _mediaService.DeleteFileAsync((MediaFile)file, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
