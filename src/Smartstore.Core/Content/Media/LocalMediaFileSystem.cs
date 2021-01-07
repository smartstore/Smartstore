using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class LocalMediaFileSystem : LocalFileSystem, IMediaFileSystem
    {
        private readonly IMediaStorageConfiguration _storageConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalMediaFileSystem(IMediaStorageConfiguration storageConfig, IHttpContextAccessor httpContextAccessor)
            : base(storageConfig.RootPath)
        {
            _storageConfig = storageConfig;
            _httpContextAccessor = httpContextAccessor;

            // Create required folders
            TryCreateDirectory("Storage");
            TryCreateDirectory("Thumbs");
            TryCreateDirectory("QueuedEmailAttachment");
        }

        /// <summary>
        /// Maps a relative path into the public path.
        /// </summary>
        /// <param name="path">The relative path to be mapped.</param>
        /// <returns>The relative path combined with the public path in an URL friendly format ('/' character for directory separator).</returns>
        protected virtual string MapPublic(string path)
        {
            return string.IsNullOrEmpty(path) ? _storageConfig.PublicPath : WebUtility.UrlDecode(PathCombine(_storageConfig.PublicPath, path).Replace(Path.DirectorySeparatorChar, '/'));
        }

        private IUrlHelper UrlHelper
        {
            get => _httpContextAccessor.HttpContext?.RequestServices?.GetRequiredService<IUrlHelper>();
        }

        #region IMediaFileSystem

        public bool IsCloudStorage => _storageConfig.IsCloudStorage;

        public string MapToPublicUrl(IFile file, bool forCloud = false)
        {
            Guard.NotNull(file, nameof(file));
            return MapPublic(file.SubPath);
        }

        public string MapToPublicUrl(string path, bool forCloud = false)
        {
            return MapPublic(path);
        }

        public virtual string MapUrlToStoragePath(string url)
        {
            if (url.IsEmpty())
            {
                return null;
            }

            var path = url;

            if (path.StartsWith("~/"))
            {
                var urlHelper = UrlHelper;
                if (urlHelper != null)
                {
                    path = urlHelper?.Content(path);
                }
            }

            if (path.StartsWith(_storageConfig.PublicPath))
            {
                return WebUtility.UrlDecode(path[_storageConfig.PublicPath.Length..]);
            }
            else
            {
                return path;
            } 
        }

        public string PublicPath => _storageConfig.PublicPath;

        public string StoragePath => _storageConfig.StoragePath;

        #endregion
    }
}
