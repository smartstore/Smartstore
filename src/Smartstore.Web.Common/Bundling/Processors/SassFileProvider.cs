using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bundling.Processors
{
    /// <summary>
    /// Knows about special virtual pathes like ".app/themevars.scss", ".app/moduleimports.scss" etc.
    /// </summary>
    internal class SassFileProvider : IFileProvider
    {
        private readonly IApplicationContext _appContext;

        public SassFileProvider(IApplicationContext appContext)
        {
            _appContext = Guard.NotNull(appContext, nameof(appContext));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
            => NotFoundDirectoryContents.Singleton;

        public IFileInfo GetFileInfo(string subpath)
        {
            if (subpath.StartsWith("themevars.scss"))
            {
                var services = EngineContext.Current.Scope;
                var repo = services.Resolve<ThemeVariableRepository>();
                var theme = services.Resolve<IThemeContext>().WorkingThemeName;
                var storeId = services.Resolve<IStoreContext>().CurrentStore.Id;

                return new ThemeVarsFileInfo(subpath, theme, storeId, repo);
            }
            else if (subpath.StartsWith("moduleimports.scss"))
            {
                // TODO: Implement module importer for SassFileProvider
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            if (filter.StartsWith("themevars.scss"))
            {
                var services = EngineContext.Current.Scope;
                var theme = services.Resolve<IThemeContext>().WorkingThemeName;
                var storeId = services.Resolve<IStoreContext>().CurrentStore.Id;
                var cts = WebCacheInvalidator.GetThemeVarsToken(theme, storeId);

                return new CancellationChangeToken(cts.Token);
            }
            else if (filter.StartsWith("moduleimports.scss"))
            {
                // TODO: Implement module importer watcher in SassFileProvider
            }

            return null;
        }
    }
}
