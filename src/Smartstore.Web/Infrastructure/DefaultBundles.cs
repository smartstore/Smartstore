using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Engine;
using Smartstore.Web.Bundling;
using WebOptimizer;

namespace Smartstore.Web.Infrastructure
{
    internal class DefaultBundles : IBundleProvider
    {
        public int Priority => 0;

        public void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline)
        {
            // ...
        }
    }
}
