using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Base modular file provider.
    /// </summary>
    public abstract class ModularFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath)
            => ResolveFileProvider(ref subpath).GetFileInfo(subpath);

        public IDirectoryContents GetDirectoryContents(string subpath)
            => ResolveFileProvider(ref subpath).GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter)
            => ResolveFileProvider(ref filter).Watch(filter);

        /// <summary>
        /// Implementors should analyze path segments and resolve matching file providers and rebase given <paramref name="path"/> accordingly.
        /// </summary>
        protected abstract IFileProvider ResolveFileProvider(ref string path);
    }
}
