using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smartstore.Core.Theming;
using Smartstore.Engine;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    internal class BundlingConfigurer : Disposable, IConfigureOptions<WebOptimizerOptions>
    {
        private readonly IApplicationContext _appContext;
        private readonly IOptionsMonitorCache<WebOptimizerOptions> _options;
        private readonly ThemeSettings _themeSettings;

        private IDisposable _callback;

        public BundlingConfigurer(
            IApplicationContext appContext, 
            IOptionsMonitorCache<WebOptimizerOptions> options,
            ThemeSettings themeSettings)
        {
            _appContext = appContext;
            _options = options;
            _themeSettings = themeSettings;
        }

        public void Configure(WebOptimizerOptions options)
        {
            // TODO: (mh) (core) Update WebOptimizerOptions whenever theme settings change by calling this method from controller with current options.

            _callback = _appContext.Configuration.GetReloadToken().RegisterChangeCallback(_ =>
            {
                _options.TryRemove(Options.DefaultName);
            }, null);

            var section = _appContext.Configuration.GetSection("WebOptimizer");
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

            options.AllowEmptyBundle ??= false;
            options.EnableCaching ??= !env.IsDevelopment();
            options.EnableMemoryCache ??= true;
            options.EnableTagHelperBundling ??= bundlingEnabled ?? !env.IsDevelopment();
            options.EnableDiskCache ??= diskCachingEnabled ?? !env.IsDevelopment();
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
