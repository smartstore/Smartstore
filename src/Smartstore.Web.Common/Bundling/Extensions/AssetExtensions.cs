using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Smartstore.ComponentModel;
using Smartstore.Web.Bundling.Processors;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    public static class AssetExtensions
    {
        const string IncludedFilesKey = "IncludedFiles";

        private static MethodInfo _expandGlobsMethod = null;

        /// <summary>
        /// Runs the DouglasCrockford JavaScript minifier on the content (instead of running NUglify).
        /// </summary>
        public static IAsset MinifyJavaScriptWithJsMin(this IAsset asset)
        {
            return Guard.NotNull(asset, nameof(asset)).AddProcessor(new CrockfordJsMinProcessor());
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

        /// <summary>
        /// Adds processors to the asset pipeline.
        /// </summary>
        public static IAsset AddProcessor(this IAsset asset, params IProcessor[] processors)
        {
            Guard.NotNull(asset, nameof(asset));

            asset.Processors.AddRange(processors);
            return asset;
        }

        /// <summary>
        /// Adds processors to the asset pipeline.
        /// </summary>
        public static IEnumerable<IAsset> AddProcessor(this IEnumerable<IAsset> assets, params IProcessor[] processors)
        {
            Guard.NotNull(assets, nameof(assets));

            assets.Each(x => x.AddProcessor(processors));
            return assets;
        }

        /// <summary>
        /// Adds Sass processor to the asset pipeline.
        /// </summary>
        public static IAsset AddSassProcessor(this IAsset asset)
        {
            Guard.NotNull(asset, nameof(asset));

            asset.AddProcessor(new SassProcessor());
            return asset;
        }

        /// <summary>
        /// Adds Sass processor to the asset pipeline.
        /// </summary>
        public static IEnumerable<IAsset> AddSassProcessor(this IEnumerable<IAsset> assets)
        {
            Guard.NotNull(assets, nameof(assets));

            assets.Each(x => x.AddProcessor(new SassProcessor()));
            return assets;
        }

        public static IEnumerable<string> GetIncludedFiles(this IAsset asset)
        {
            Guard.NotNull(asset, nameof(asset));
            return asset.Items.Get(IncludedFilesKey) as IEnumerable<string>;
        }

        public static void SetIncludedFiles(this IAsset asset, IEnumerable<string> files)
        {
            Guard.NotNull(asset, nameof(asset));
            asset.Items[IncludedFilesKey] = files;
        }

        public static IEnumerable<string> ExpandGlobPatterns(this IAsset asset, IWebHostEnvironment env)
        {
            var invoker = GetExpandGlobsInvoker();
            var result = invoker.Invoke(null, new object[] { asset, env }) as IEnumerable<string>;
            return result;
        }

        private static FastInvoker GetExpandGlobsInvoker()
        {
            _expandGlobsMethod ??= typeof(IAsset).Assembly
                .GetLoadableTypes()
                .FirstOrDefault(x => x.Name == "Asset")
                .GetMethod("ExpandGlobs", BindingFlags.Public | BindingFlags.Static);

            return FastInvoker.GetInvoker(_expandGlobsMethod);
        }
    }
}
