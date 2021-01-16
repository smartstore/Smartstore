using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Data;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.Imaging;
using Smartstore.Imaging.Adapters.ImageSharp;
using Smartstore.Threading;

namespace Smartstore.Core.Bootstrapping
{
    public class MediaStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            //// Utils
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

            // ImageSharp adapter factory
            builder.RegisterType<SharpImageFactory>().As<IImageFactory>().SingleInstance();
            builder.RegisterType<ImageCache>().As<IImageCache>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultImageProcessor>().As<IImageProcessor>().InstancePerLifetimeScope();

            // Register factory for currently active media storage provider
            if (appContext.IsInstalled)
            {
                builder.Register(MediaStorageProviderFactory);
            }
            else
            {
                builder.Register<Func<IMediaStorageProvider>>(c => () =>
                    new FileSystemMediaStorageProvider(
                        c.ResolveNamed<IMediaFileSystem>("local"),
                        c.Resolve<AsyncRunner>()));
            }

            // Register all album providers
            var albumProviderTypes = appContext.TypeScanner.FindTypes<IAlbumProvider>(ignoreInactiveModules: true);
            foreach (var type in albumProviderTypes)
            {
                builder.RegisterType(type).As<IAlbumProvider>().Keyed<IAlbumProvider>(type).InstancePerLifetimeScope();
            }

            // Register all track detectors
            var trackDetectorTypes = appContext.TypeScanner.FindTypes<IMediaTrackDetector>(ignoreInactiveModules: true);
            foreach (var type in trackDetectorTypes)
            {
                builder.RegisterType(type).As<IMediaTrackDetector>().Keyed<IMediaTrackDetector>(type).InstancePerLifetimeScope();
            }

            // Handlers
            builder.RegisterType<ImageHandler>().As<IMediaHandler>().InstancePerLifetimeScope();
        }

        private static Func<IMediaStorageProvider> MediaStorageProviderFactory(IComponentContext c)
        {
            var systemName = c.Resolve<ISettingService>().GetSettingByKey("Media.Storage.Provider", FileSystemMediaStorageProvider.SystemName);
            var provider = c.Resolve<IProviderManager>().GetProvider<IMediaStorageProvider>(systemName);
            return () => provider.Value;
        }
    }
}
