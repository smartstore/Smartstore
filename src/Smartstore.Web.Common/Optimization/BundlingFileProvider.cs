using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.IO;

namespace Smartstore.Web.Optimization
{
    public class BundlingFileProvider : IFileProvider
    {
        private static readonly ConcurrentDictionary<string, string> _minFiles = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly IFileSystem _inner;

        public BundlingFileProvider(IFileSystem inner)
        {
            _inner = Guard.NotNull(inner, nameof(inner));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _inner.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return _inner.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return _inner.Watch(filter);
        }
    }
}
