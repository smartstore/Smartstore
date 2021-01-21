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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalMediaFileSystem(IHttpContextAccessor httpContextAccessor, IMediaStorageConfiguration storageConfiguration)
            : base(storageConfiguration.RootPath)
        {
            _httpContextAccessor = httpContextAccessor;
            StorageConfiguration = storageConfiguration;

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
            return string.IsNullOrEmpty(path) 
                ? StorageConfiguration.PublicPath 
                : WebUtility.UrlDecode(PathCombine(StorageConfiguration.PublicPath, path).Replace(Path.DirectorySeparatorChar, '/'));
        }

        private IUrlHelper UrlHelper
        {
            get => _httpContextAccessor.HttpContext?.RequestServices?.GetRequiredService<IUrlHelper>();
        }

        #region IMediaFileSystem

        public IMediaStorageConfiguration StorageConfiguration { get; }

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

            if (path.StartsWith(StorageConfiguration.PublicPath))
            {
                return WebUtility.UrlDecode(path[StorageConfiguration.PublicPath.Length..]);
            }
            else
            {
                return path;
            } 
        }

        #endregion
    }
}
