using Autofac;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.Imaging;
using Smartstore.Imaging.Adapters.ImageSharp;
using Smartstore.Imaging.Barcodes;
using Smartstore.Imaging.Barcodes.Impl;

namespace Smartstore.Core.Bootstrapping
{
    internal class MediaStarter : StarterBase
    {
        public MediaStarter()
        {
            RunAfter<TaskSchedulerStarter>();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
            {
                if (builder.ApplicationContext.IsInstalled)
                {
                    app.MapMedia();
                }
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // Utils
            builder.RegisterType<MediaHelper>().InstancePerLifetimeScope();
            builder.RegisterType<MediaExceptionFactory>().InstancePerLifetimeScope();

            // Register IMediaFileSystem twice, this time explicitly named.
            // We may need this later in decorator classes as a kind of fallback.
            builder.RegisterType<MediaStorageConfiguration>().As<IMediaStorageConfiguration>().SingleInstance();
            builder.RegisterType<LocalMediaFileSystem>().As<IMediaFileSystem>().SingleInstance();
            builder.RegisterType<LocalMediaFileSystem>().Named<IMediaFileSystem>("local").SingleInstance();

            builder.RegisterType<MediaTypeResolver>().As<IMediaTypeResolver>().InstancePerLifetimeScope();
            builder.RegisterType<MediaUrlGenerator>().As<IMediaUrlGenerator>().InstancePerLifetimeScope();
            builder.RegisterType<AlbumRegistry>().As<IAlbumRegistry>().InstancePerLifetimeScope();
            builder.RegisterType<FolderService>().As<IFolderService>().InstancePerLifetimeScope();
            builder.RegisterType<MediaTracker>().As<IMediaTracker>().InstancePerLifetimeScope();
            builder.RegisterType<MediaSearcher>().As<IMediaSearcher>().InstancePerLifetimeScope();
            builder.RegisterType<MediaService>().As<IMediaService>().InstancePerLifetimeScope();
            builder.RegisterType<MediaMover>().As<IMediaMover>().InstancePerLifetimeScope();
            builder.RegisterType<DownloadService>().As<IDownloadService>().InstancePerLifetimeScope();
            builder.RegisterType<ImageOffloader>().As<IImageOffloder>().InstancePerLifetimeScope();
            builder.RegisterType<MediaDupeDetectorFactory>().As<IMediaDupeDetectorFactory>().InstancePerLifetimeScope();

            // ImageSharp adapter factory
            builder.RegisterType<SharpImageFactory>().As<IImageFactory>().SingleInstance();
            builder.RegisterType<ImageCache>().As<IImageCache>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultImageProcessor>().As<IImageProcessor>().InstancePerLifetimeScope();

            // Barcode & QrCode
            builder.RegisterType<DefaultBarcodeEncoder>().As<IBarcodeEncoder>().SingleInstance();

            // Register factory for currently active media storage provider
            builder.Register(MediaStorageProviderFactory);

            // Register all album providers
            var albumProviderTypes = appContext.TypeScanner.FindTypes<IAlbumProvider>();
            foreach (var type in albumProviderTypes)
            {
                builder.RegisterType(type).As<IAlbumProvider>().Keyed<IAlbumProvider>(type).InstancePerLifetimeScope();
            }

            // Register all track detectors
            var trackDetectorTypes = appContext.TypeScanner.FindTypes<IMediaTrackDetector>();
            foreach (var type in trackDetectorTypes)
            {
                builder.RegisterType(type).As<IMediaTrackDetector>().Keyed<IMediaTrackDetector>(type).InstancePerLifetimeScope();
            }

            // Handlers
            builder.RegisterType<ImageHandler>().As<IMediaHandler>().InstancePerLifetimeScope();

            // Icons
            builder.RegisterType<IconExplorer>().As<IIconExplorer>().SingleInstance();
        }

        private static Func<IMediaStorageProvider> MediaStorageProviderFactory(IComponentContext c)
        {
            var systemName = c.Resolve<ISettingService>().GetSettingByKey("Media.Storage.Provider", FileSystemMediaStorageProvider.SystemName);
            var provider = c.Resolve<IProviderManager>().GetProvider<IMediaStorageProvider>(systemName);
            return () => provider.Value;
        }
    }
}
