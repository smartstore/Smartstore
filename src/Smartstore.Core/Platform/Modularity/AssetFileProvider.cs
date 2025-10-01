using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity
{
    public class AssetFileProvider : ModularFileProvider, IAssetFileProvider
    {
        private readonly IFileSystem _webRoot;
        private readonly Dictionary<string, IFileProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<string, IApplicationContext, IFileProvider>> _segmentedProviders = new(StringComparer.OrdinalIgnoreCase);

        public AssetFileProvider(IFileSystem webRoot)
        {
            _webRoot = Guard.NotNull(webRoot);
        }

        public void AddFileProvider(string pathPrefix, IFileProvider provider)
        {
            Guard.NotEmpty(pathPrefix);
            Guard.NotNull(provider);

            pathPrefix = pathPrefix.TrimStart('/').EnsureEndsWith('/');
            _providers[pathPrefix] = provider;
        }

        public void AddSegmentedFileProvider(string pathPrefix, Func<string, IApplicationContext, IFileProvider> resolver)
        {
            Guard.NotEmpty(pathPrefix);
            Guard.NotNull(resolver);

            pathPrefix = pathPrefix.TrimStart('/').EnsureEndsWith('/');
            _segmentedProviders[pathPrefix] = resolver;
        }

        protected override IFileProvider ResolveFileProvider(ref string path)
        {
            var path2 = path.TrimStart(PathUtility.PathSeparators);

            var index = path2.IndexOf('/');

            if (index > -1 && path2.Length > index)
            {
                // Get first segment including leading slash, e.g. "themes/"
                var firstSegment = path2[..(index + 1)].ToLowerInvariant();
                var lenBase = firstSegment.Length;

                // First check complex providers (that require segment)
                if (_segmentedProviders.TryGetValue(firstSegment, out var resolver))
                {
                    // Get next segment, this time without leading slash, e.g. "Flex"
                    var nextSegment = path2[(index + 1)..];
                    index = nextSegment.IndexOf('/');
                    if (index > -1 && nextSegment.Length > index)
                    {
                        nextSegment = nextSegment[..index];
                        lenBase += nextSegment.Length;
                    }

                    if (nextSegment.Length > 0)
                    {
                        var provider = resolver(nextSegment, EngineContext.Current.Application);
                        if (provider != null)
                        {
                            // Rebase path by stripping found segments
                            path = path2[lenBase..];
                            return provider;
                        }
                    }
                }

                // Then check simple providers (no segment required)
                if (_providers.TryGetValue(firstSegment, out var simpleProvider))
                {
                    // Simple provider found - just strip the prefix and return
                    path = path2[lenBase..];
                    return simpleProvider;
                }
            }

            return _webRoot;
        }
    }
}
