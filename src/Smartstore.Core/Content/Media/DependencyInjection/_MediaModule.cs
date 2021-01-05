using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Core.Content.Media;
using Smartstore.Engine;

namespace Smartstore.Core.DependencyInjection
{
    public class MediaModule : Autofac.Module
    {
        private readonly ITypeScanner _typeScanner;

        public MediaModule(ITypeScanner typeScanner)
        {
            _typeScanner = typeScanner;
        }

        protected override void Load(ContainerBuilder builder)
        {
            //// Utils
            //builder.RegisterType<MediaMigrator>().InstancePerRequest();
            //builder.RegisterType<MediaMigrator3>().InstancePerRequest();
            //builder.RegisterType<MediaHelper>().InstancePerRequest();
            //builder.RegisterType<MediaExceptionFactory>().InstancePerRequest();

            builder.RegisterType<MediaTypeResolver>().As<IMediaTypeResolver>().InstancePerLifetimeScope();
            //builder.RegisterType<MediaUrlGenerator>().As<IMediaUrlGenerator>().InstancePerRequest();
            builder.RegisterType<AlbumRegistry>().As<IAlbumRegistry>().InstancePerLifetimeScope();
            builder.RegisterType<FolderService>().As<IFolderService>().InstancePerLifetimeScope();
            //builder.RegisterType<MediaTracker>().As<IMediaTracker>().InstancePerRequest();
            //builder.RegisterType<MediaSearcher>().As<IMediaSearcher>().InstancePerRequest();
            //builder.RegisterType<MediaService>().As<IMediaService>().InstancePerRequest();
            //builder.RegisterType<DownloadService>().As<IDownloadService>().InstancePerRequest();

            //// ImageProcessor adapter factory
            //builder.RegisterType<IPImageFactory>().As<IImageFactory>().SingleInstance();

            //builder.RegisterType<ImageCache>().As<IImageCache>().InstancePerRequest();
            //builder.RegisterType<DefaultImageProcessor>().As<IImageProcessor>().InstancePerRequest();
            //builder.RegisterType<MediaMover>().As<IMediaMover>().InstancePerRequest();

            //// Register factory for currently active media storage provider
            //if (DataSettings.DatabaseIsInstalled())
            //{
            //    builder.Register(MediaStorageProviderFactory);
            //}
            //else
            //{
            //    builder.Register<Func<IMediaStorageProvider>>(c => () => new FileSystemMediaStorageProvider(new MediaFileSystem()));
            //}

            //// Register all album providers
            //var albumProviderTypes = _typeFinder.FindClassesOfType<IAlbumProvider>(ignoreInactivePlugins: true);
            //foreach (var type in albumProviderTypes)
            //{
            //    builder.RegisterType(type).As<IAlbumProvider>().Keyed<IAlbumProvider>(type).InstancePerRequest();
            //}

            //// Handlers
            //builder.RegisterType<ImageHandler>().As<IMediaHandler>().InstancePerRequest();
        }

        //private static Func<IMediaStorageProvider> MediaStorageProviderFactory(IComponentContext c)
        //{
        //    var systemName = c.Resolve<ISettingService>().GetSettingByKey("Media.Storage.Provider", FileSystemMediaStorageProvider.SystemName);
        //    var provider = c.Resolve<IProviderManager>().GetProvider<IMediaStorageProvider>(systemName);
        //    return () => provider.Value;
        //}
    }
}
