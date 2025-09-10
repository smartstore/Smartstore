#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.IO;

namespace Smartstore.Core.AI.Metadata
{
    public class AIMetadataLoader : IAIMetadataLoader
    {
        private readonly IMemoryCache _cache;
        private readonly IApplicationContext _appContext;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public AIMetadataLoader(IMemoryCache cache, IApplicationContext appContext)
        {
            _cache = cache;
            _appContext = appContext;
        }

        public AIMetadata LoadMetadata(string rootPath)
        {
            Guard.NotEmpty(rootPath);

            var path = PathUtility.Join(rootPath, "metadata.json");
            
            var result = _cache.GetOrCreate(path.ToLower(), entry =>
            {
                var file = _appContext.ContentRoot.GetFile(path);
                if (!file.Exists)
                {
                    throw new InvalidOperationException("Invalid root path.");
                }

                var json = file.ReadAllText();
                if (Deserialize(json) is not AIMetadata metadata)
                {
                    throw new InvalidOperationException("Failed to deserialize AIMetadata.");
                }

                // Obtain a change token from the file provider whose
                // callback is triggered when the file is modified.
                var changeToken = _appContext.ContentRoot.Watch(path);
                if (changeToken != null)
                {
                    entry.AddExpirationToken(changeToken);
                }

                return metadata;
            });

            return result!;
        }

        protected AIMetadata? Deserialize(string json)
        {
            return JsonSerializer.Deserialize<AIMetadata>(json, _serializerOptions);
        }
    }
}
