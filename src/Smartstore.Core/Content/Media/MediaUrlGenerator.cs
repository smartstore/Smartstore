using System.Globalization;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Net;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaUrlGenerator : IMediaUrlGenerator
    {
        const string _fallbackImagesRootPath = "images/";

        private readonly MediaSettings _mediaSettings;
        private readonly string _host;
        private readonly string _pathBase;
        private readonly string _fallbackImageFileName;
        private readonly string _processedImagesRootPath;

        public MediaUrlGenerator(
            IMediaStorageConfiguration storageConfig,
            ISettingService settingService,
            MediaSettings mediaSettings,
            IStoreContext storeContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _mediaSettings = mediaSettings;
            _processedImagesRootPath = storageConfig.PublicPath;

            var httpContext = httpContextAccessor.HttpContext;
            string pathBase = "/";

            if (httpContext != null)
            {
                var request = httpContext.Request;
                pathBase = request.PathBase;

                var cdn = storeContext.CurrentStore.ContentDeliveryNetwork;
                if (cdn.HasValue() && !CommonHelper.IsDevEnvironment && !httpContext.Connection.IsLocal())
                {
                    _host = cdn;
                }
                else if (mediaSettings.AutoGenerateAbsoluteUrls)
                {
                    _host = "//{0}{1}".FormatInvariant(request.Host, pathBase);
                }
                else
                {
                    _host = pathBase;
                }
            }

            _host = _host.EmptyNull().EnsureEndsWith('/');
            _pathBase = pathBase.EnsureEndsWith('/');
            _fallbackImageFileName = settingService.GetSettingByKey("Media.DefaultImageName", "default-image.png");
        }

        public static string FallbackImagesRootPath => _fallbackImagesRootPath;

        public virtual string GenerateUrl(
            MediaFileInfo file,
            QueryString query = default,
            string host = null,
            bool doFallback = true)
        {
            string path;

            // Build virtual path with pattern "media/{id}/{album}/{dir}/{NameWithExt}"
            if (file?.Path != null)
            {
                path = _processedImagesRootPath + file.Id.ToString(CultureInfo.InvariantCulture) + "/" + file.Path;
            }
            else if (doFallback)
            {
                path = _processedImagesRootPath + "0/" + _fallbackImageFileName;
            }
            else
            {
                return null;
            }

            if (host == null)
            {
                host = _host;
            }
            else if (host == string.Empty)
            {
                host = _pathBase;
            }
            else
            {
                host = host.EnsureEndsWith('/');
            }

            var url = host;

            // Strip leading "/", the host/pathBase has this already
            if (path[0] == '/')
            {
                path = path[1..];
            }

            // Append media path
            url += path;

            // Append file hash to query
            if (file != null && _mediaSettings.AppendFileVersionToUrl)
            {
                query = query.Add("ver", ETagUtility.GenerateETag(file.LastModified, file.Length, null, true));
            }

            // Append query to url
            if (query.HasValue)
            {
                url += query.ToString();
            }

            return url;
        }
    }
}