using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.IO;

namespace Smartstore.Web.Assets
{
    public class AssetFileProvider : IFileProvider
    {
        private readonly IFileSystem _inner;

        public AssetFileProvider(IFileSystem inner)
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
