using System;
using System.Collections.Generic;
using WebOptimizer;
using Smartstore.Web.Bundling.Processors;
using Smartstore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AssetPipelineExtensions
    {
        /// <summary>
        /// Runs the DouglasCrockford JavaScript minifier on the content (instead of running NUglify).
        /// </summary>
        public static IAsset MinifyJavaScriptWithJsMin(this IAsset asset)
        {
            Guard.NotNull(asset, nameof(asset));

            asset.Processors.Add(new CrockfordJsMinProcessor());
            return asset;
        }

        /// <summary>
        /// Runs the DouglasCrockford JavaScript minifier on the content (instead of running NUglify).
        /// </summary>
        public static IEnumerable<IAsset> MinifyJavaScriptWithJsMin(this IEnumerable<IAsset> assets)
        {
            Guard.NotNull(assets, nameof(assets));

            assets.Each(x => x.Processors.Add(new CrockfordJsMinProcessor()));
            return assets;
        }
    }
}
