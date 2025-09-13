#nullable enable

using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Smartstore.ComponentModel;
using Smartstore.IO;

namespace Smartstore.Core.AI.Metadata
{
    public class AIMetadataLoader : IAIMetadataLoader
    {
        private readonly IMemoryCache _cache;
        private readonly IApplicationContext _appContext;
        private readonly JsonSerializerSettings _serializerSettings;

        public AIMetadataLoader(IMemoryCache cache, IApplicationContext appContext)
        {
            _cache = cache;
            _appContext = appContext;

            _serializerSettings = JsonConvert.DefaultSettings!();
            _serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            _serializerSettings.ContractResolver = new SmartContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
        }

        public AIMetadata LoadMetadata(IFile file)
        {
            Guard.NotNull(file);
            
            var cacheKey = (file.PhysicalPath ?? file.SubPath).ToLower();
            var result = _cache.GetOrCreate(cacheKey, entry =>
            {
                if (!file.Exists)
                {
                    throw new InvalidOperationException($"Metadata file {cacheKey} does not exist.");
                }

                var json = file.ReadAllText();
                if (Deserialize(json) is not AIMetadata metadata)
                {
                    throw new InvalidOperationException("Failed to deserialize AIMetadata.");
                }

                // Obtain a change token from the file provider whose
                // callback is triggered when the file is modified.
                var changeToken = file.FileSystem.Watch(file.SubPath);
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
            return JsonConvert.DeserializeObject<AIMetadata>(json, _serializerSettings);
        }
    }
}
