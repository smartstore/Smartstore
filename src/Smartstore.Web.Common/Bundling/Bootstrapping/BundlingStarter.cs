using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Smartstore.Core.Theming;
using Smartstore.Engine.Builders;
using Smartstore.IO;
using Smartstore.Web.Bundling;
using Smartstore.Web.Bundling.Processors;

namespace Smartstore.Web.Bootstrapping
{
    internal class BundlingStarter : StarterBase
    {
        public BundlingStarter()
        {
            RunAfter<MvcStarter>();
        }

        private static IFileProvider ResolveThemeFileProvider(string themeName, IApplicationContext appContext)
        {
            var themeRegistry = appContext.Services.Resolve<IThemeRegistry>();
            return themeRegistry?.GetThemeDescriptor(themeName)?.WebRoot;
        }

        private static IFileProvider ResolveModuleFileProvider(string moduleName, IApplicationContext appContext)
        {
            return appContext.ModuleCatalog.GetModuleByName(moduleName, false)?.WebRoot;
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // Configure & register asset file provider
            var assetFileProvider = new AssetFileProvider(appContext.WebRoot);

            assetFileProvider.AddFileProvider("themes/", ResolveThemeFileProvider);
            assetFileProvider.AddFileProvider("modules/", ResolveModuleFileProvider);
            assetFileProvider.AddFileProvider(".app/", new SassFileProvider(appContext));

            builder.RegisterInstance<IAssetFileProvider>(assetFileProvider);
            builder.RegisterType<BundlingOptionsConfigurer>().As<IConfigureOptions<BundlingOptions>>().SingleInstance();
            builder.RegisterType<BundleContextAccessor>().As<IBundleContextAccessor>().SingleInstance();

            builder.RegisterType<BundleCollection>().As<IBundleCollection>().SingleInstance();
            builder.RegisterType<DefaultBundleBuilder>().As<IBundleBuilder>().SingleInstance();
            builder.RegisterType<BundleCache>().As<IBundleCache>().SingleInstance();
            builder.RegisterType<BundleDiskCache>().As<IBundleDiskCache>().SingleInstance();
            builder.RegisterType<BundleTagGenerator>().As<IAssetTagGenerator>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
            {
                app.UseWhen(ctx => ctx.Request.IsGet(), x =>
                {
                    x.UseMiddleware<BundleMiddleware>();
                });

                var bundles = app.ApplicationServices.GetRequiredService<IBundleCollection>();
                var publisher = new BundlePublisher();
                publisher.RegisterBundles(builder.ApplicationContext, bundles);
            });

            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = builder.ApplicationBuilder.ApplicationServices.GetRequiredService<IAssetFileProvider>(),
                    ContentTypeProvider = MimeTypes.ContentTypeProvider
                });
            });
        }
    }
}
