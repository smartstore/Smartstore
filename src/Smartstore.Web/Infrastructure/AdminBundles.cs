using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Web.Bundling;
using WebOptimizer;

namespace Smartstore.Web.Infrastructure
{
    internal class AdminBundles : IBundleProvider
    {
        public int Priority => 0;

        public void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline)
        {
            RegisterDataGrid(assetPipeline);
        }

        private IAsset RegisterDataGrid(IAssetPipeline assetPipeline)
        {
            var sourceFiles = new[]
            {
                "/components/datagrid/datagrid.js",
                "/components/datagrid/datagrid-pager.js",
                "/components/datagrid/datagrid-tools.js",
                "/js/smartstore.editortemplates.js"
            };

            var bundle = assetPipeline
                .AddBundle("/bundles/js/datagrid.js", "text/javascript; charset=UTF-8", sourceFiles)
                .EnforceFileExtensions(".js", ".jsx", ".es5", ".es6")
                //.MinifyJavaScriptWithJsMin()
                .AddResponseHeader("X-Content-Type-Options", "nosniff")
                .Concatenate();
            return bundle;
        }
    }
}
