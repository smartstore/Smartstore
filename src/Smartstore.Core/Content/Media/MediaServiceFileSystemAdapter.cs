﻿using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// A media service implementation that emulates file system.
    /// </summary>
    public partial class MediaServiceFileSystemAdapter(
        IMediaService mediaService,
        IMediaSearcher mediaSearcher,
        IFolderService folderService,
        IMediaStorageConfiguration storageConfig,
        MediaHelper mediaHelper,
        MediaExceptionFactory exceptionFactory) : FileSystemBase, IMediaFileSystem
    {
        private readonly IMediaService _mediaService = mediaService;
        private readonly IMediaSearcher _mediaSearcher = mediaSearcher;
        private readonly IFolderService _folderService = folderService;
        private readonly MediaHelper _mediaHelper = mediaHelper;
        private readonly IMediaStorageProvider _storageProvider = mediaService.StorageProvider;
        private readonly MediaExceptionFactory _exceptionFactory = exceptionFactory;
        private readonly string _mediaRootPath = storageConfig.PublicPath;

        protected string Fix(string path)
            => path.Replace('\\', '/');

        public IMediaStorageConfiguration StorageConfiguration { get; } = storageConfig;

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

        protected internal override Task<AsyncOut<string>> CheckUniqueFileNameCore(string subpath, bool async)
        {
            return _mediaService.CheckUniqueFileNameAsync(subpath);
        }

        public override bool DirectoryExists(string subpath)
        {
            return _mediaService.FolderExists(subpath);
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

            return _mediaService.ConvertMediaFolder(node);
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

        #endregion
    }
}
