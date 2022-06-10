using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smartstore.Core.Theming;
using Smartstore.Web.Bundling;

namespace Smartstore.Web.Bootstrapping
{
    internal class BundlingOptionsConfigurer : Disposable, IConfigureOptions<BundlingOptions>
    {
        private readonly IApplicationContext _appContext;
        private readonly IAssetFileProvider _fileProvider;
        private readonly IOptionsMonitorCache<BundlingOptions> _optionsCache;
        private readonly Work<ThemeSettings> _themeSettings;

        private BundlingOptions _prevOptions;
        private IDisposable _callback;

        public BundlingOptionsConfigurer(
            IApplicationContext appContext,
            IAssetFileProvider fileProvider,
            IOptionsMonitorCache<BundlingOptions> optionsCache,
            Work<ThemeSettings> themeSettings)
        {
            _appContext = appContext;
            _fileProvider = fileProvider;
            _optionsCache = optionsCache;
            _themeSettings = themeSettings;
        }

        public void Configure(BundlingOptions options)
        {
            var section = _appContext.Configuration.GetSection("Bundling");
            section.Bind(options);

            var env = _appContext.HostEnvironment;
            var themeSettings = _appContext.IsInstalled ? _themeSettings.Value : new ThemeSettings { BundleOptimizationEnabled = 1, AssetCachingEnabled = 1 };

            bool? bundlingEnabled = null;
            if (themeSettings.BundleOptimizationEnabled > 0)
            {
                bundlingEnabled = themeSettings.BundleOptimizationEnabled > 1;
            }

            bool? diskCachingEnabled = null;
            if (themeSettings.AssetCachingEnabled > 0)
            {
                diskCachingEnabled = themeSettings.AssetCachingEnabled > 1;
            }

            if (options.EnableAutoprefixer == null && options.Autoprefixer.AlwaysDisableInDevMode && env.IsDevelopment())
            {
                options.EnableAutoprefixer = false;
            }

            options.EnableBundling ??= bundlingEnabled ?? !env.IsDevelopment();
            options.EnableClientCache ??= !env.IsDevelopment();
            options.EnableDiskCache ??= diskCachingEnabled ?? !env.IsDevelopment();
            options.EnableMinification ??= bundlingEnabled ?? !env.IsDevelopment();
            options.EnableAutoprefixer ??= bundlingEnabled ?? !env.IsDevelopment();
            options.FileProvider ??= _fileProvider;

            if (_prevOptions != null)
            {
                // It's an appsettings.json change. Check if we need to invalidate the cache.
                if (_prevOptions.EnableMinification != options.EnableMinification || _prevOptions.EnableAutoprefixer != options.EnableAutoprefixer)
                {
                    // Cannot pass in ctor --> circular dependency exception!
                    _appContext.Services.Resolve<IBundleCache>().ClearAsync().Await();
                }
            }

            _callback = _appContext.Configuration.GetReloadToken().RegisterChangeCallback(OnChange, options);
            _prevOptions = options;
        }

        private void OnChange(object state)
        {
            _prevOptions = (BundlingOptions)state;
            _optionsCache.TryRemove(Options.DefaultName);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _callback?.Dispose();
            }
        }
    }
}
