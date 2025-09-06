#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Smartstore.Core.AI.Metadata
{
    public class AIMetadataLoader : IAIMetadataLoader
    {
        private readonly IMemoryCache _cache;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public AIMetadataLoader(IMemoryCache cache)
        {
            _cache = cache;
        }

        public AIMetadata LoadMetadata(string rootPath)
        {
            Guard.NotEmpty(rootPath);

            var path = Path.GetDirectoryName(rootPath);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Invalid root path.");
            }

            var result = _cache.GetOrCreate(path, entry =>
            {
                var filePath = Path.Combine(path, "metadata.json");
                var json = File.ReadAllText(rootPath);

                if (Deserialize(json) is not AIMetadata metadata)
                {
                    throw new InvalidOperationException("Failed to deserialize AIMetadata.");
                }

                // TODO: Set file monitoring to invalidate cache if file changes

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
