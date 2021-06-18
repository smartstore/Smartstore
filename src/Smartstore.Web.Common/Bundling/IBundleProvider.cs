using System;
using Smartstore.Engine;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    public interface IBundleProvider
    {
        void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline);

        int Priority { get; }
    }
}
