using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;
using Smartstore.Net;
using Smartstore.Web.Bundling;

namespace Smartstore.Web.Razor
{
    public class SmartFileVersionProvider : IFileVersionProvider
    {
        private const string VersionKey = "v";

        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _cache;

        public SmartFileVersionProvider(
            IAssetFileProvider assetFileProvider, 
            TagHelperMemoryCacheProvider cacheProvider)
        {
            _fileProvider = assetFileProvider;
            _cache = cacheProvider.Cache;
        }

        public string AddFileVersionToPath(PathString requestPathBase, string path)
        {
            var resolvedPath = path;

            var queryStringOrFragmentStartIndex = path.AsSpan().IndexOfAny('?', '#');
            if (queryStringOrFragmentStartIndex != -1)
            {
                resolvedPath = path[..queryStringOrFragmentStartIndex];
            }

            if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                // Don't append version if the path is absolute.
                return path;
            }

            if (_cache.TryGetValue<string>(path, out var value) && value is not null)
            {
                return value;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.AddExpirationToken(_fileProvider.Watch(resolvedPath));
            var fileInfo = _fileProvider.GetFileInfo(resolvedPath);

            if (!fileInfo.Exists &&
                requestPathBase.HasValue &&
                resolvedPath.StartsWithNoCase(requestPathBase.Value))
            {
                var requestPathBaseRelativePath = resolvedPath.Substring(requestPathBase.Value.Length);
                cacheEntryOptions.AddExpirationToken(_fileProvider.Watch(requestPathBaseRelativePath));
                fileInfo = _fileProvider.GetFileInfo(requestPathBaseRelativePath);
            }

            if (fileInfo.Exists)
            {
                value = QueryHelpers.AddQueryString(path, VersionKey, GetHashForFile(fileInfo));
            }
            else
            {
                // if the file is not in the current server.
                value = path;
            }

            cacheEntryOptions.SetSize(value.Length * sizeof(char));
            _cache.Set(path, value, cacheEntryOptions);
            return value;
        }

        private static string GetHashForFile(IFileInfo fileInfo)
        {
            if (fileInfo is IFileHashProvider hashProvider)
            {
                return hashProvider.GetFileHashAsync().Await().ToString("x");
            }
            else
            {
                return ETagUtility.GenerateETag(fileInfo.LastModified, fileInfo.Length, null, true);
            }
        }
    }
}
