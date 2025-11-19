#nullable enable

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Smartstore.ComponentModel;

namespace Smartstore.Core.AI.Metadata
{
    public class JsonAIMetadataLoader : IAIMetadataLoader
    {
        private readonly IMemoryCache _cache;
        private readonly IApplicationContext _appContext;
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonAIMetadataLoader(IMemoryCache cache, IApplicationContext appContext)
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

        public AIMetadata LoadMetadata(string moduleSystemName)
        {
            Guard.NotEmpty(moduleSystemName);

            var cacheKey = "aimetadata:" + moduleSystemName;

            var result = _cache.GetOrCreate(cacheKey, entry =>
            {
                var (metadata, changeToken) = LoadMetadataCore(moduleSystemName);
                if (changeToken != null)
                {
                    // Register the change token to invalidate the cache entry when the file changes.
                    entry.AddExpirationToken(changeToken);
                }

                return metadata;
            });

            return result!;
        }

        protected virtual (AIMetadata, IChangeToken?) LoadMetadataCore(string moduleSystemName)
        {
            var module = _appContext.ModuleCatalog.GetModuleByName(moduleSystemName) ?? throw new InvalidOperationException($"Module {moduleSystemName} does not exist.");
            var file = module.ContentRoot.GetFile("metadata.json");
            if (!file.Exists)
            {
                throw new InvalidOperationException($"Metadata file for {moduleSystemName} not found.");
            }

            var json = file.ReadAllText();
            if (Deserialize(json) is not AIMetadata metadata)
            {
                throw new InvalidOperationException("Failed to deserialize AIMetadata.");
            }

            // Obtain a change token from the file provider whose
            // callback is triggered when the file is modified.
            var changeToken = file.FileSystem.Watch(file.SubPath);

            return (metadata, changeToken);
        }

        protected virtual AIMetadata? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<AIMetadata>(json, _serializerSettings);
        }

        public void Invalidate(string moduleSystemName)
        {
            _cache.Remove("aimetadata:" + moduleSystemName);
        }
    }
}
