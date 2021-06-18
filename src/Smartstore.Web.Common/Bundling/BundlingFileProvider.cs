using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Utilities;
using StackExchange.Redis;

namespace Smartstore.Web.Bundling
{
    public class BundlingFileProvider : IFileProvider
    {
        private static readonly ConcurrentDictionary<string, string> _minFiles = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly IFileProvider _inner;

        public BundlingFileProvider(IFileProvider inner)
        {
            _inner = Guard.NotNull(inner, nameof(inner));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _inner.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return _inner.GetFileInfo(TryFindMinFile(subpath));
        }

        public IChangeToken Watch(string filter)
        {
            return _inner.Watch(TryFindMinFile(filter));
        }

        private string TryFindMinFile(string subpath)
        {
            if (CommonHelper.IsDevEnvironment)
            {
                // return path as is in debug mode
                return subpath;
            }
            
            return subpath;
        }
    }
}
