using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
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

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
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
                var assetFileProvider = app.ApplicationServices.GetRequiredService<IAssetFileProvider>();
                assetFileProvider.AddFileProvider(".app/", new SassFileProvider(builder.ApplicationContext));

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = assetFileProvider,
                    ContentTypeProvider = MimeTypes.ContentTypeProvider,
                });

                // Server static files from ".well-known" folder (e.g. for verification files)
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new ExpandedFileSystem(".well-known", builder.ApplicationContext.WebRoot),
                    RequestPath = "/.well-known",
                    ServeUnknownFileTypes = true,
                    // Some text-based verification files have no extension
                    DefaultContentType = "text/plain"
                });
            });
        }
    }
}
