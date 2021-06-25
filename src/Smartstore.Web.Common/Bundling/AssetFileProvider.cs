using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Engine;

namespace Smartstore.IO
{
    public interface IAssetFileProvider : IFileProvider
    {
        string PathCombine(params string[] paths);
    }
    
    public class AssetFileProvider : IAssetFileProvider
    {
        private readonly IFileSystem _webRoot;
        private readonly Dictionary<string, Func<string, IApplicationContext, IFileProvider>> _providers = new(StringComparer.OrdinalIgnoreCase);

        public AssetFileProvider(IFileSystem webRoot)
        {
            _webRoot = Guard.NotNull(webRoot, nameof(webRoot));
        }

        public virtual IDirectoryContents GetDirectoryContents(string subpath)
        {
            return ResolveFileProvider(ref subpath).GetDirectoryContents(subpath);
        }

        public virtual IFileInfo GetFileInfo(string subpath)
        {
            return ResolveFileProvider(ref subpath).GetFileInfo(subpath);
        }

        public virtual IChangeToken Watch(string filter)
        {
            return ResolveFileProvider(ref filter).Watch(filter);
        }

        public virtual string PathCombine(params string[] paths)
        {
            return _webRoot.PathCombine(paths);
        }

        /// <summary>
        /// Adds a file provider for a given path prefix.
        /// </summary>
        /// <param name="pathPrefix">Path prefix, e.g.: "themes/", "modules/" etc.</param>
        public void AddFileProvider(string pathPrefix, IFileProvider provider)
        {
            AddFileProvider(pathPrefix, (a, b) => Guard.NotNull(provider, nameof(provider)));
        }

        /// <summary>
        /// Adds a file provider resolver delegate for a given path prefix.
        /// </summary>
        /// <param name="pathPrefix">Path prefix, e.g.: "themes/", "modules/" etc.</param>
        /// <param name="resolver">The provider resolver delegate. First string argument provides the next path token.</param>
        public void AddFileProvider(string pathPrefix, Func<string, IApplicationContext, IFileProvider> resolver)
        {
            Guard.NotEmpty(pathPrefix, nameof(pathPrefix));
            Guard.NotNull(resolver, nameof(resolver));

            pathPrefix = pathPrefix.TrimStart('/').EnsureEndsWith('/');
            _providers[pathPrefix] = resolver;
        }

        protected virtual IFileProvider ResolveFileProvider(ref string path)
        {
            var path2 = path.TrimStart(PathHelper.PathSeparators);
            var index = path2.IndexOf('/');
            var lenBase = 0;

            if (index > -1 && path2.Length > index)
            {
                // Get first segment including leading slash, e.g. "themes/"
                var firstSegment = path2.Substring(0, index + 1).ToLowerInvariant();
                lenBase = firstSegment.Length;

                if (_providers.TryGetValue(firstSegment, out var resolver))
                {
                    // Get next segment, this time without leading slash, e.g. "Flex"
                    var nextSegment = path2[(index + 1)..];
                    index = nextSegment.IndexOf('/');
                    if (index > -1 && nextSegment.Length > index)
                    {
                        nextSegment = nextSegment.Substring(0, index);
                        lenBase += nextSegment.Length;
                    }

                    if (nextSegment.Length > 0)
                    {
                        var provider = resolver(nextSegment, EngineContext.Current.Application);
                        if (provider != null)
                        {
                            // Re-root path by stripping found segments
                            path = path2[lenBase..];
                            return provider;
                        }
                    }
                }
            }

            return _webRoot;
        }
    }
}
