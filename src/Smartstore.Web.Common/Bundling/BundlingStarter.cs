using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUglify.JavaScript;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Web.Bundling
{
    internal class BundlingStarter : StarterBase
    {
        public BundlingStarter()
        {
            RunAfter<MvcStarter>();
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            var cssBundlingSettings = new CssBundlingSettings { Minify = false };
            var codeBundlingSettings = new CodeBundlingSettings { Minify = false };
            var codeSettings = new CodeSettings { IgnoreAllErrors = false, MinifyCode = false, ScriptVersion = ScriptVersion.EcmaScript6, EvalLiteralExpressions = false, AmdSupport = true };
            codeSettings.IgnoreErrorCollection.Add("JS1010");

            services.AddWebOptimizer((IWebHostEnvironment)appContext.HostEnvironment, cssBundlingSettings, codeBundlingSettings, p => {
                var asset = p.AddJavaScriptBundle("/bundle/js/datagrid.js",
                    "components/datagrid/datagrid.js",
                    "components/datagrid/datagrid-pager.js",
                    "components/datagrid/datagrid-tools.js",
                    "js/smartstore.editortemplates.js")
                .Concatenate()
                //.MinifyJavaScriptWithJsMin()
                .FingerprintUrls();
            });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
            {
                app.UseWebOptimizer();
            });
        }
    }
}
