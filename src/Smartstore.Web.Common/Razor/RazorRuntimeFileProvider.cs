using Autofac;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Theming;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Web.Razor
{
    /// <summary>
    /// A file provider that is aware of module and theme paths. If an incoming path
    /// points to an extension resource, this provider will truncate the path and pass it
    /// to the extension's file provider (see <see cref="IExtensionLocation.ContentRoot"/>).
    /// </summary>
    public class RazorRuntimeFileProvider : ModularFileProvider
    {
        private readonly IApplicationContext _appContext;
        private readonly bool _ignoreThemes;

        public RazorRuntimeFileProvider(IApplicationContext appContext, bool ignoreThemes = false)
        {
            _appContext = Guard.NotNull(appContext);
            _ignoreThemes = ignoreThemes;
        }

        protected override IFileProvider ResolveFileProvider(ref string path)
        {
            if (WebHelper.IsExtensionPath(path, out var type, out var name, out var remaining))
            {
                IExtensionLocation extension = type == ExtensionType.Module
                    ? _appContext.ModuleCatalog?.GetModuleByName(name)
                    : (_ignoreThemes ? null : _appContext.Services.ResolveOptional<IThemeRegistry>()?.GetThemeDescriptor(name));

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
