using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class LocalMediaFileSystem : LocalFileSystem, IMediaFileSystem
    {
        public LocalMediaFileSystem(IMediaStorageConfiguration storageConfiguration)
            : base(storageConfiguration.RootPath)
        {
            StorageConfiguration = storageConfiguration;

            // Create required folders
            TryCreateDirectory("Storage");
            TryCreateDirectory("Thumbs");
            TryCreateDirectory("QueuedEmailAttachment");
        }

        public IMediaStorageConfiguration StorageConfiguration { get; }
    }
}