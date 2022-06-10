using System.IO;
using System.Threading.Tasks;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;

namespace Smartstore.Core.Tests.Content.Media.Storage
{
    public class TestDatabaseMediaStorageProvider : IMediaStorageProvider
    {
        public Stream OpenRead(MediaFile media)
        {
            return new MemoryStream(media.MediaStorage.Data);
        }

        public bool IsCloudStorage { get; } = false;

        public string GetPublicUrl(MediaFile mediaFile)
        {
            return null;
        }

        public byte[] Load(MediaFile media)
        {
            return media.MediaStorage.Data;
        }

        public Task<byte[]> LoadAsync(MediaFile media)
        {
            return Task.FromResult(Load(media));
        }

        public void Save(MediaFile media, MediaStorageItem item)
        {
        }

        public Task SaveAsync(MediaFile media, MediaStorageItem item)
        {
            return Task.CompletedTask;
        }

        public void Remove(params MediaFile[] medias)
        {
        }

        public long GetSize(MediaFile media)
        {
            return media.MediaStorage?.Data?.Length ?? 0;
        }

        public void ChangeExtension(MediaFile mediaFile, string extension)
        {
        }

        public Task<long> GetLengthAsync(MediaFile mediaFile)
        {
            return Task.FromResult(GetSize(mediaFile));
        }

        public Task<Stream> OpenReadAsync(MediaFile mediaFile)
        {
            return Task.FromResult(OpenRead(mediaFile));
        }

        public Task RemoveAsync(params MediaFile[] mediaFiles)
        {
            return Task.CompletedTask;
        }

        public Task ChangeExtensionAsync(MediaFile mediaFile, string extension)
        {
            return Task.CompletedTask;
        }
    }
}
