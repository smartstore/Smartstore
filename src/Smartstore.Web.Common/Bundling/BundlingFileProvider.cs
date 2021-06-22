using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

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
            if (subpath.Contains("_alert"))
            {
                var yo = true;
            }

            return _inner.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            if (filter.Contains("_alert"))
            {
                var yo = true;
            }
            
            return _inner.Watch(filter);
        }
    }
}
