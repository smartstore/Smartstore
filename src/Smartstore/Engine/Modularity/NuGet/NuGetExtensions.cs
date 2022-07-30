using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Smartstore.Engine.Modularity.NuGet
{
    internal static class NuGetExtensions
    {
        private static readonly string[] PackageBaseAddressUrl = { "PackageBaseAddress/3.0.0" };

        public static SourceRepository GetCoreV3Custom(this Repository.RepositoryFactory factory, PackageSource source)
        {
            var providers = Repository.Provider.GetCoreV3()
                .Concat(new[] { new Lazy<INuGetResourceProvider>(() => new HttpHandlerResourceV3NoProxyProvider()) });

            return Repository.CreateSource(providers, source);
        }

        public static Task<JObject> GetJObjectAsync(this HttpSource source,
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

        private static Task<JObject> ProcessJson(HttpSourceResult result)
        {
            return LoadJsonAsync(result.Stream, false);
        }

        private static async Task<JObject> LoadJsonAsync(Stream stream, bool leaveOpen)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen))
            using (var jsonReader = new JsonTextReader(reader))
            {
                // Avoid error prone json.net date handling
                jsonReader.DateParseHandling = DateParseHandling.None;

                var json = await JObject.LoadAsync(jsonReader, new JsonLoadSettings()
                {
                    LineInfoHandling = LineInfoHandling.Ignore,
                    CommentHandling = CommentHandling.Ignore,
                });

                return json;
            }
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
