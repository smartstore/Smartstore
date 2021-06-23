using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using WebOptimizer;

namespace Smartstore.Web.Assets
{
    internal class AssetStarter : StarterBase
    {
        public AssetStarter()
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
            var fileProvider = new AssetFileProvider(appContext.WebRoot);
            var publisher = new BundlePublisher();

            services.AddWebOptimizer(environment, cssBundlingSettings, jsBundlingSettings, assetPipeline => 
            {
                publisher.RegisterBundles(appContext, assetPipeline);

                foreach (var asset in assetPipeline.Assets)
                {
                    if (asset.Items.ContainsKey("fileprovider") || asset.Items.ContainsKey("usecontentroot"))
                    {
                        // A custom file provider was added already, leave it alone.
                        continue;
                    }
                    
                    asset.UseFileProvider(fileProvider);
                }
            });

            services.AddNodeServices(o => 
            {
                // TODO: (core) Configure NodeServices?
            });

            services.AddTransient<IConfigureOptions<WebOptimizerOptions>, AssetConfigurer>();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterDecorator<SmartAssetBuilder, IAssetBuilder>();
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
