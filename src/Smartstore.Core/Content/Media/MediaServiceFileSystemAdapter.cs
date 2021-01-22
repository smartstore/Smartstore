using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.IO;
using Smartstore.Collections;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Dasync.Collections;
using Smartstore.Utilities;

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
            IMediaStorageConfiguration storageConfig,
            MediaHelper mediaHelper,
            MediaExceptionFactory exceptionFactory)
        {
            _mediaService = mediaService;
            _mediaSearcher = mediaSearcher;
            _folderService = folderService;
            _mediaHelper = mediaHelper;
            _storageProvider = mediaService.StorageProvider;
            _exceptionFactory = exceptionFactory;
            _mediaRootPath = storageConfig.PublicPath;
            StorageConfiguration = storageConfig;
        }

        protected string Fix(string path)
            => path.Replace('\\', '/');

        public IMediaStorageConfiguration StorageConfiguration { get; }

        public bool IsCloudStorage => _storageProvider.IsCloudStorage;

        public string MapUrlToStoragePath(string url)
        {
            url = Fix(url).TrimStart('/');

            if (!url.StartsWith(_mediaRootPath, StringComparison.OrdinalIgnoreCase))
            {
                // Is a folder path, no need to strip off public URL stuff.
                return url;
            }

            // Strip off root, e.g. "media/"
            var path = url[_mediaRootPath.Length..];

            // Strip off media id from path, e.g. "123/"
            var firstSlashIndex = path.IndexOf('/');

            return path[firstSlashIndex..];
        }

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

            var count = await EnumerateFilesAsync(subpath, pattern, deep).LongCountAsync();
            return count;
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
            return EnumerateFiles(subpath, pattern, deep)
                .OfType<IFileEntry>()
                .Concat(EnumerateDirectories(subpath, pattern, deep));
        }

        public override async IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string subpath = null, string pattern = "*", bool deep = false)
        {
            await foreach (var entry in EnumerateFilesAsync(subpath, pattern, deep))
            {
                yield return entry;
            }

            await foreach (var entry in EnumerateDirectoriesAsync(subpath, pattern, deep))
            {
                yield return entry;
            }
        }

        public override IEnumerable<IFile> EnumerateFiles(string subpath = null, string pattern = "*", bool deep = false)
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

            var files = _mediaSearcher.SearchFiles(query)
                .Load()
                .AsEnumerable()
                .Select(_mediaService.ConvertMediaFile)
                .OfType<IFile>();

            return files;
        }

        public override async IAsyncEnumerable<IFile> EnumerateFilesAsync(string subpath = null, string pattern = "*", bool deep = false)
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

            var result = await _mediaService.SearchFilesAsync(query);

            foreach (var file in result.OfType<IFile>())
            {
                yield return file;
            }
        }

        public override IEnumerable<IDirectory> EnumerateDirectories(string subpath = null, string pattern = "*", bool deep = false)
        {
            var node = _folderService.GetNodeByPath(subpath);
            if (node == null)
            {
                throw _exceptionFactory.FolderNotFound(subpath);
            }

            Wildcard wildcard = pattern.IsEmpty() || pattern == "*" 
                ? null 
                : new Wildcard(pattern);

            var folders = deep ? node.FlattenNodes(false) : node.Children;
            var result = folders
                .Where(MatchesPattern)
                .Select(x => new MediaFolderInfo(x))
                .OfType<IDirectory>();

            return result;

            bool MatchesPattern(TreeNode<MediaFolderNode> node)
            {
                return wildcard == null ? true : wildcard.IsMatch(node.Value.Name);
            }
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
            => throw new NotImplementedException();

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
