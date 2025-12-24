#nullable enable

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Smartstore.Caching;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.AI.Metadata
{
    public class JsonAIMetadataLoader : IAIMetadataLoader
    {
        private readonly IMemoryCache _cache;
        private readonly IApplicationContext _appContext;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonAIMetadataLoader(IMemoryCache cache, IApplicationContext appContext)
        {
            _cache = cache;
            _appContext = appContext;

            _jsonOptions = SmartJsonOptions.CamelCased;
        }

        protected static string BuildCacheKey(string moduleSystemName)
            => "aimetadata:" + moduleSystemName;

        protected IMemoryCache Cache
        {
            get => _cache;
        }

        public AIMetadata LoadMetadata(string moduleSystemName)
        {
            Guard.NotEmpty(moduleSystemName);

            var cacheKey = BuildCacheKey(moduleSystemName);

            var result = _cache.GetOrCreate(cacheKey, entry =>
            {
                var (metadata, changeToken) = LoadMetadataCore(moduleSystemName);
                if (changeToken != null)
                {
                    // Register the change token to invalidate the cache entry when the file changes.
                    entry.AddExpirationToken(changeToken);
                }

                return new CacheEntry { Key = cacheKey, Value = metadata, ValueType = typeof(AIMetadata) };
            });

            return (AIMetadata)result!.Value;
        }

        public virtual Task<AIMetadata?> PostProcessAsync(AIMetadata localMetadata)
        {
            localMetadata.PostProcessed = true;
            return Task.FromResult<AIMetadata?>(null);
        }

        public void ReplaceMetadata(string moduleSystemName, AIMetadata metadata)
        {
            Guard.NotEmpty(moduleSystemName);
            Guard.NotNull(metadata);

            if (_cache.TryGetValue(BuildCacheKey(moduleSystemName), out CacheEntry? entry))
            {
                entry!.Value = metadata;
            }
        }

        protected virtual (AIMetadata, IChangeToken?) LoadMetadataCore(string moduleSystemName)
        {
            var module = _appContext.ModuleCatalog.GetModuleByName(moduleSystemName) ?? throw new InvalidOperationException($"Module {moduleSystemName} does not exist.");
            var file = module.ContentRoot.GetFile("metadata.json");
            if (!file.Exists)
            {
                throw new InvalidOperationException($"Metadata file for {moduleSystemName} not found.");
            }

            if (Deserialize(file) is not AIMetadata metadata)
            {
                throw new InvalidOperationException("Failed to deserialize AIMetadata.");
            }

            // Obtain a change token from the file provider whose
            // callback is triggered when the file is modified.
            var changeToken = file.FileSystem.Watch(file.SubPath);

            return (metadata, changeToken);
        }

        protected virtual AIMetadata? Deserialize(IFile file)
        {
            using var stream = file.OpenRead();
            return JsonSerializer.Deserialize<AIMetadata>(stream, _jsonOptions);
        }

        public void Invalidate(string moduleSystemName)
        {
            _cache.Remove(BuildCacheKey(moduleSystemName));
        }
    }
}
