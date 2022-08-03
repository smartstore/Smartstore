using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class LocalMediaFileSystem : LocalFileSystem, IMediaFileSystem
    {
        public LocalMediaFileSystem(IMediaStorageConfiguration storageConfiguration, IApplicationContext appContext)
            : base(EnsureRootDirectoryCreated(storageConfiguration, appContext))
        {
            StorageConfiguration = storageConfiguration;

            // Create required folders
            this.TryCreateDirectory("Storage");
            this.TryCreateDirectory("Thumbs");
            this.TryCreateDirectory("QueuedEmailAttachment");
        }

        public IMediaStorageConfiguration StorageConfiguration { get; }

        public bool IsCloudStorage => StorageConfiguration.IsCloudStorage;

        private static string EnsureRootDirectoryCreated(IMediaStorageConfiguration storageConfiguration, IApplicationContext appContext)
        {
            if (!storageConfiguration.StoragePathIsAbsolute)
            {
                appContext.ContentRoot.TryCreateDirectory(storageConfiguration.StoragePath);
            }

            return storageConfiguration.RootPath;
        }
    }
}