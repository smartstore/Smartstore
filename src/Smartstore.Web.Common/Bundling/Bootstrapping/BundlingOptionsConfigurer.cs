using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smartstore.Core.Theming;
using Smartstore.Engine;
using Smartstore.Web.Bundling;

namespace Smartstore.Web.Bootstrapping
{
    internal class BundlingOptionsConfigurer : Disposable, IConfigureOptions<BundlingOptions>
    {
        private readonly IApplicationContext _appContext;
        private readonly IBundleFileProvider _fileProvider;
        private readonly IOptionsMonitorCache<BundlingOptions> _optionsCache;
        private readonly ThemeSettings _themeSettings;

        private IDisposable _callback;

        public BundlingOptionsConfigurer(
            IApplicationContext appContext,
            IBundleFileProvider fileProvider,
            IOptionsMonitorCache<BundlingOptions> optionsCache,
            ThemeSettings themeSettings)
        {
            _appContext = appContext;
            _fileProvider = fileProvider;
            _optionsCache = optionsCache;
            _themeSettings = themeSettings;
        }

        public void Configure(BundlingOptions options)
        {
            // TODO: (mh) (core) Update BundlingOptions whenever theme settings change by calling this method from controller with current options.

            _callback = _appContext.Configuration.GetReloadToken().RegisterChangeCallback(_ =>
            {
                _optionsCache.TryRemove(Options.DefaultName);
            }, null);

            var section = _appContext.Configuration.GetSection("Bundling");
            section.Bind(options);

            var env = _appContext.HostEnvironment;
            var cacheDirectory = section["cacheDirectory"];

            bool? bundlingEnabled = null;
            if (_themeSettings.BundleOptimizationEnabled > 0)
            {
                bundlingEnabled = _themeSettings.BundleOptimizationEnabled > 1;
            }

            bool? diskCachingEnabled = null;
            if (_themeSettings.AssetCachingEnabled > 0)
            {
                diskCachingEnabled = _themeSettings.AssetCachingEnabled > 1;
            }

            options.EnableBundling ??= bundlingEnabled ?? !env.IsDevelopment();
            options.EnableClientCache ??= !env.IsDevelopment();
            options.EnableMemoryCache ??= true;
            options.EnableDiskCache ??= diskCachingEnabled ?? !env.IsDevelopment();
            options.EnableMinification ??= bundlingEnabled ?? !env.IsDevelopment();
            options.EnableAutoPrefixer ??= bundlingEnabled ?? !env.IsDevelopment();
            options.FileProvider ??= _fileProvider;
            options.CacheDirectory = cacheDirectory.IsEmpty()
                ? _appContext.TenantRoot.GetDirectory("AssetCache").PhysicalPath
                : cacheDirectory;
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
