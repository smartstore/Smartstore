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
    internal class SmartFileVersionProvider : IFileVersionProvider
    {
        private const string VersionKey = "v";

        public SmartFileVersionProvider(
            IAssetFileProvider assetFileProvider, 
            TagHelperMemoryCacheProvider cacheProvider)
        {
            FileProvider = assetFileProvider;
            Cache = cacheProvider.Cache;
        }

        public IFileProvider FileProvider { get; }

        public IMemoryCache Cache { get; }

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

            if (Cache.TryGetValue<string>(path, out var value) && value is not null)
            {
                return value;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.AddExpirationToken(FileProvider.Watch(resolvedPath));
            var fileInfo = FileProvider.GetFileInfo(resolvedPath);

            if (!fileInfo.Exists &&
                requestPathBase.HasValue &&
                resolvedPath.StartsWithNoCase(requestPathBase.Value))
            {
                var requestPathBaseRelativePath = resolvedPath.Substring(requestPathBase.Value.Length);
                cacheEntryOptions.AddExpirationToken(FileProvider.Watch(requestPathBaseRelativePath));
                fileInfo = FileProvider.GetFileInfo(requestPathBaseRelativePath);
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
            Cache.Set(path, value, cacheEntryOptions);
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
