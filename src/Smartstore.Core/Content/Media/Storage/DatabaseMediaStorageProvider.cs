using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Media.Storage
{
    [SystemName("MediaStorage.SmartStoreDatabase")]
    [FriendlyName("Database")]
    [Order(0)]
    public class DatabaseMediaStorageProvider : IMediaStorageProvider, IMediaSender, IMediaReceiver
    {
        private readonly SmartDbContext _db;

        public DatabaseMediaStorageProvider(SmartDbContext db)
        {
            _db = db;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public static string SystemName => "MediaStorage.SmartStoreDatabase";

        protected Stream OpenBlobStream(int mediaStorageId)
        {
            return _db.DataProvider.OpenBlobStream<MediaStorage, byte[]>(x => x.Data, mediaStorageId);
        }

        #region IMediaStorageProvider

        public bool IsCloudStorage
            => false;

        public Task<long> GetLengthAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            var id = mediaFile.MediaStorageId ?? 0;
            if (id == 0)
            {
                return Task.FromResult(0L);
            }

            if (_db.DataProvider.CanStreamBlob)
            {
                using var stream = OpenBlobStream(id);
                return Task.FromResult(stream.Length);
            }
            else
            {
                return Task.FromResult(mediaFile.MediaStorage?.Data?.LongLength ?? 0);
            }
        }

        public Stream OpenRead(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (_db.DataProvider.CanStreamBlob)
            {
                if (mediaFile.MediaStorageId > 0)
                {
                    return OpenBlobStream(mediaFile.MediaStorageId.Value);
                }

                return null;
            }
            else
            {
                return mediaFile.MediaStorage?.Data?.ToStream();
            }
        }

        public Task<Stream> OpenReadAsync(MediaFile mediaFile)
            => Task.FromResult<Stream>(OpenRead(mediaFile));

        public async Task<byte[]> LoadAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (mediaFile.MediaStorageId == null)
            {
                return Array.Empty<byte>();
            }

            if (_db.DataProvider.CanStreamBlob)
            {
                using (var stream = OpenBlobStream(mediaFile.MediaStorageId.Value))
                {
                    return await stream.ToByteArrayAsync();
                }
            }
            else
            {
                return mediaFile.MediaStorage?.Data;
            }
        }

        public async Task SaveAsync(MediaFile mediaFile, MediaStorageItem item)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (item == null)
            {
                mediaFile.ApplyBlob(null);
                await _db.SaveChangesAsync();
                return;
            }

            using (item)
            {
                if (_db.DataProvider.CanStreamBlob)
                {
                    await SaveFast(mediaFile, item);
                }
                else
                {
                    // BLOB stream unsupported
                    var buffer = await item.SourceStream.ToByteArrayAsync();
                    mediaFile.ApplyBlob(buffer);
                    mediaFile.Size = buffer.Length;
                    await _db.SaveChangesAsync();
                }
            }
        }

        private async Task<int> SaveFast(MediaFile mediaFile, MediaStorageItem item)
        {
            var sourceStream = item.SourceStream;
            mediaFile.Size = (int)sourceStream.Length;

            if (mediaFile.MediaStorageId == null)
            {
                // Insert new blob
                var sql = "INSERT INTO [MediaStorage] (Data) Values(@p0)";
                mediaFile.MediaStorageId = await _db.DataProvider.InsertIntoAsync(sql, sourceStream);
            }
            else
            {
                // Update existing blob
                var sql = "UPDATE [MediaStorage] SET [Data] = @p0 WHERE Id = @p1";
                await _db.Database.ExecuteSqlRawAsync(sql, sourceStream, mediaFile.MediaStorageId.Value);
            }

            return mediaFile.MediaStorageId.Value;
        }

        public async Task RemoveAsync(params MediaFile[] mediaFiles)
        {
            foreach (var media in mediaFiles)
            {
                media.ApplyBlob(null);
            }

            if (mediaFiles.Length > 0)
            {
                await _db.SaveChangesAsync();
            }
        }

        public Task ChangeExtensionAsync(MediaFile mediaFile, string extension)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        #endregion

        #region IMediaSender, IMediaReceiver

        public async Task MoveToAsync(IMediaReceiver target, MediaMoverContext context, MediaFile mediaFile)
        {
            Guard.NotNull(target, nameof(target));
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (mediaFile.MediaStorageId != null)
            {
                // Let target store data (into a file for example)
                await target.ReceiveAsync(context, mediaFile, await OpenReadAsync(mediaFile));

                // Remove blob from DB
                try
                {
                    mediaFile.MediaStorageId = null;
                    mediaFile.MediaStorage = null;
                    // Remove with stub entity
                    _db.MediaStorage.Remove(new MediaStorage { Id = mediaFile.MediaStorageId.Value });
                }
                catch { }
            }
        }

        public Task ReceiveAsync(MediaMoverContext context, MediaFile mediaFile, Stream stream)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(mediaFile, nameof(mediaFile));

            // Store data for later bulk commit
            if (stream != null && stream.Length > 0)
            {
                // Requires AutoDetectChanges set to true or remove explicit entity detaching
                return SaveAsync(mediaFile, MediaStorageItem.FromStream(stream));
            }

            return Task.CompletedTask;
        }

        Task IMediaSender.OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            if (succeeded && context.AffectedFiles.Any() && _db.DataProvider.CanShrink)
            {
                // SHrink database after sending/removing at least one blob.
                return _db.DataProvider.ShrinkDatabaseAsync(cancelToken);
            }
            
            return Task.CompletedTask;
        }

        Task IMediaReceiver.OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            // nothing to do
            return Task.CompletedTask;
        }

        #endregion
    }
}