using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Smartstore.Engine.Modularity.NuGet
{
    internal static class NuGetExtensions
    {
        private static readonly string[] PackageBaseAddressUrl = { "PackageBaseAddress/3.0.0" };

        public static Task<JsonObject> GetJsonObjectAsync(this HttpSource source,
            Uri uri,
            HttpSourceCacheContext cacheContext,
            ILogger log,
            CancellationToken token)
        {
            var cacheKey = GetHashKey(uri);

            var request = new HttpSourceCachedRequest(uri.AbsoluteUri, cacheKey, cacheContext)
            {
                EnsureValidContents = stream => LoadJsonAsync(stream, true).Await(),
                IgnoreNotFounds = false
            };

            return source.GetAsync(request, ProcessJson, log, token);
        }

        public static Task<HttpSourceResult> GetNupkgAsync(this HttpSource source,
            Uri uri,
            HttpSourceCacheContext cacheContext,
            ILogger log,
            CancellationToken cancelToken)
        {
            var cacheKey = GetHashKey(uri);

            var request = new HttpSourceCachedRequest(uri.AbsoluteUri, cacheKey, cacheContext)
            {
                IgnoreNotFounds = false,
                EnsureValidContents = stream =>
                {
                    using (var reader = new PackageArchiveReader(stream, leaveStreamOpen: true))
                    {
                        reader.NuspecReader.GetIdentity();
                    }
                }
            };

            return source.GetAsync(request, result => Task.FromResult(result), log, cancelToken);
        }

        private static string GetHashKey(Uri uri)
        {
            return uri.AbsolutePath
                .Replace('/', '_')
                .Replace('\\', '_')
                .Replace(':', '_');
        }

        private static Task<JsonObject> ProcessJson(HttpSourceResult result)
        {
            return LoadJsonAsync(result.Stream, false);
        }

        private static async Task<JsonObject> LoadJsonAsync(Stream stream, bool leaveOpen)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: leaveOpen);
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using var doc = await JsonDocument.ParseAsync(reader.BaseStream, options).ConfigureAwait(false);
            var node = JsonNode.Parse(doc.RootElement.GetRawText());

            if (node is not JsonObject obj)
            {
                throw new InvalidDataException("Expected a JSON object.");
            }

            return obj;
        }

        public static Uri GetPackageBaseAddressUri(this ServiceIndexResourceV3 serviceIndex)
        {
            return serviceIndex.GetServiceUri(PackageBaseAddressUrl);
        }

        public static Uri GetServiceUri(this ServiceIndexResourceV3 serviceIndex, string[] types)
        {
            var uris = serviceIndex.GetServiceEntryUris(types);

            if (uris.Count < 1)
            {
                throw new InvalidDataException($"Unable to find a service of type: {string.Join(", ", types)}. Verify the index.json file contains this entry.");
            }

            return uris[0];
        }
    }
}
