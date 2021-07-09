using System;
using System.Collections.Generic;
using Autofac;
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
                var bundleContext = _appContext.Services.Resolve<IBundleContextAccessor>().BundleContext;

                if (bundleContext != null && bundleContext.DataTokens.TryGetValue("ThemeVars", out var obj) && obj is IDictionary<string, object> themeVars)
                {
                    // ThemeVars already passed to bundler context. Apparently we're in validation mode.
                    return new ThemeVarsFileInfo(themeVars, repo);
                }
                else
                {
                    var theme = services.Resolve<IThemeContext>().CurrentTheme.ThemeName;
                    var storeId = _appContext.Services.Resolve<IStoreContext>().CurrentStore.Id;
                    return new ThemeVarsFileInfo(subpath, theme, storeId,repo);
                }
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
                var theme = services.Resolve<IThemeContext>().CurrentTheme.ThemeName;
                var storeId = services.Resolve<IStoreContext>().CurrentStore.Id;
                var cts = ThemeVariableRepository.GetToken(theme, storeId);

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
