using Autofac;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
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
            if (subpath.StartsWith(ThemeVarsFileInfo.FileName))
            {
                var services = EngineContext.Current.Scope;
                var repo = services.Resolve<ThemeVariableRepository>();
                var bundleContext = GetBundleContext();

                if (bundleContext != null && bundleContext.DataTokens.TryGetValue("ThemeVars", out var obj) && obj is IDictionary<string, object> themeVars)
                {
                    // ThemeVars already passed to bundler context. Apparently we're in validation mode.
                    return new ThemeVarsFileInfo(themeVars, repo);
                }
                else
                {
                    var theme = services.Resolve<IThemeContext>().CurrentTheme.Name;
                    var storeId = services.Resolve<IStoreContext>().CurrentStore.Id;
                    return new ThemeVarsFileInfo(subpath, theme, storeId, repo);
                }
            }
            else if (subpath.StartsWith(ModuleImportsFileInfo.FileName))
            {
                var isThemeable = GetBundleContext()?.CacheKey.Fragments?.ContainsKey("Theme") == true;
                return new ModuleImportsFileInfo(!isThemeable);
            }

            return new NotFoundFileInfo(subpath);

            BundleContext GetBundleContext()
            {
                return _appContext.Services.Resolve<IBundleContextAccessor>().BundleContext;
            }
        }

        public IChangeToken Watch(string filter)
        {
            if (filter.StartsWith(ThemeVarsFileInfo.FileName))
            {
                var services = EngineContext.Current.Scope;
                var theme = services.Resolve<IThemeContext>().CurrentTheme.Name;
                var storeId = services.Resolve<IStoreContext>().CurrentStore.Id;
                var cts = ThemeVariableRepository.GetToken(theme, storeId);

                return new CancellationChangeToken(cts.Token);
            }

            return NullChangeToken.Singleton;
        }
    }
}
