using System;
using System.Linq;
using Smartstore.Engine;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    internal class BundlePublisher
    {
        public void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline)
        {
            Guard.NotNull(appContext, nameof(appContext));
            Guard.NotNull(assetPipeline, nameof(assetPipeline));

            var bundleProviders = appContext.TypeScanner
                .FindTypes<IBundleProvider>(ignoreInactiveModules: true)
                .Select(providerType => Activator.CreateInstance(providerType) as IBundleProvider)
                .OrderByDescending(provider => provider.Priority)
                .ToList();

            foreach (var provider in bundleProviders)
            {
                provider.RegisterBundles(appContext, assetPipeline);
            }
        }
    }
}
