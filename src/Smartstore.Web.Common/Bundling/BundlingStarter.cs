using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var isProduction = appContext.HostEnvironment.IsProduction();

            var cssBundlingSettings = new CssBundlingSettings 
            { 
                Minify = isProduction, 
                Concatenate = isProduction,
                AdjustRelativePaths = isProduction,
            };
            
            var jsBundlingSettings = new CodeBundlingSettings 
            { 
                Minify = isProduction,
                Concatenate = isProduction,
                AdjustRelativePaths = isProduction,
            };

            var codeSettings = jsBundlingSettings.CodeSettings;
            codeSettings.AmdSupport = true;
            codeSettings.IgnoreAllErrors = true;
            //codeSettings.ScriptVersion = ScriptVersion.EcmaScript6;
            codeSettings.MinifyCode = isProduction;
            codeSettings.IgnoreErrorCollection.Add("JS1010");

            var environment = (IWebHostEnvironment)appContext.HostEnvironment;
            var fileProvider = new BundlingFileProvider(environment.WebRootFileProvider);
            var publisher = new BundlePublisher();

            services.AddWebOptimizer(environment, cssBundlingSettings, jsBundlingSettings, assetPipeline => 
            {
                publisher.RegisterBundles(appContext, assetPipeline);

                foreach (var asset in assetPipeline.Assets)
                {
                    asset.UseFileProvider(fileProvider);
                }
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
