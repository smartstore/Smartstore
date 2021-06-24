using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    public class AssetFileProvider : IFileProvider
    {
        private readonly IFileSystem _webRoot;

        public AssetFileProvider(IFileSystem webRoot)
        {
            _webRoot = Guard.NotNull(webRoot, nameof(webRoot));
        }

        public virtual IDirectoryContents GetDirectoryContents(string subpath)
        {
            return ResolveFileProvider(subpath).GetDirectoryContents(subpath);
        }

        public virtual IFileInfo GetFileInfo(string subpath)
        {
            return ResolveFileProvider(subpath).GetFileInfo(subpath);
        }

        public virtual IChangeToken Watch(string filter)
        {
            return ResolveFileProvider(filter).Watch(filter);
        }

        public virtual string PathCombine(params string[] paths)
        {
            return _webRoot.PathCombine(paths);
        }

        protected virtual IFileProvider ResolveFileProvider(string path)
        {
            path = path.TrimStart(PathHelper.PathSeparators);

            var index = path.IndexOfAny(PathHelper.PathSeparators);
            if (index > -1)
            {
                var firstSegment = path.Substring(0, index).ToLowerInvariant();
                if (firstSegment == "themes")
                {
                    // TODO: (core) Implement
                }
                else if (firstSegment == "modules")
                {
                    // TODO: (core) Implement
                }
            }

            return _webRoot;
        }
    }
}
