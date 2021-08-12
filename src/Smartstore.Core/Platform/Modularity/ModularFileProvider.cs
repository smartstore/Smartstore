using System;
using Autofac;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Core.Theming;
using Smartstore.Http;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// A file provider that is aware of module and theme pathes. If an incoming path
    /// points to an extension resource, this provider will truncate the path and pass it
    /// to the extension's file provider (see <see cref="IExtensionLocation.ContentRoot"/>).
    /// </summary>
    public class ModularFileProvider : IFileProvider
    {
        private readonly IApplicationContext _appContext;
        
        public ModularFileProvider(IApplicationContext appContext)
        {
            _appContext = Guard.NotNull(appContext, nameof(appContext));
        }

        public IFileInfo GetFileInfo(string subpath)
            => ResolveFileProvider(ref subpath).GetFileInfo(subpath);

        public IDirectoryContents GetDirectoryContents(string subpath)
            => ResolveFileProvider(ref subpath).GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter)
            => ResolveFileProvider(ref filter).Watch(filter);

        private IFileProvider ResolveFileProvider(ref string path)
        {
            if (WebHelper.IsExtensionPath(path, out var type, out var name, out var remaining))
            {
                IExtensionLocation extension = type == ExtensionType.Module
                    ? _appContext.ModuleCatalog?.GetModuleByName(name)
                    : _appContext.Services.ResolveOptional<IThemeRegistry>()?.GetThemeDescriptor(name);

                if (extension?.ContentRoot != null)
                {
                    path = remaining;
                    return extension.ContentRoot;
                }
            }

            return _appContext.ContentRoot;
        }
    }
}
