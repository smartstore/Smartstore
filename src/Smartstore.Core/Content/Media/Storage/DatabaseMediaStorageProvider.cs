using System.Runtime.CompilerServices;
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

        protected virtual Stream OpenBlobStream(int mediaStorageId)
        {
            return _db.DataProvider.OpenBlobStream<MediaStorage, byte[]>(x => x.Data, mediaStorageId);
        }

        #region IMediaStorageProvider

        public bool IsCloudStorage
            => false;

        public virtual async Task<long> GetLengthAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            var id = mediaFile.MediaStorageId ?? 0;
            if (id == 0)
            {
                return 0L;
            }

            if (_db.DataProvider.CanReadSequential)
            {
                using var stream = OpenBlobStream(id);
                return stream.Length;
            }
            else
            {
                await _db.LoadReferenceAsync(mediaFile, x => x.MediaStorage);
                return mediaFile.MediaStorage?.Data?.LongLength ?? 0;
            }
        }

        public virtual Stream OpenRead(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (_db.DataProvider.CanReadSequential)
            {
                if (mediaFile.MediaStorageId > 0)
                {
                    return OpenBlobStream(mediaFile.MediaStorageId.Value);
                }

                return null;
            }
            else
            {
                _db.LoadReferenceAsync(mediaFile, x => x.MediaStorage).Await();
                return new MemoryStream(mediaFile.MediaStorage?.Data);
            }
        }

        public virtual async Task<Stream> OpenReadAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (_db.DataProvider.CanReadSequential)
            {
                if (mediaFile.MediaStorageId > 0)
                {
                    return OpenBlobStream(mediaFile.MediaStorageId.Value);
                }

                return null;
            }
            else
            {
                await _db.LoadReferenceAsync(mediaFile, x => x.MediaStorage);
                return new MemoryStream(mediaFile.MediaStorage?.Data);
            }
        }

        public virtual async Task<byte[]> LoadAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            if (mediaFile.MediaStorageId == null)
            {
                return Array.Empty<byte>();
            }

            if (_db.DataProvider.CanReadSequential)
            {
                using (var stream = OpenBlobStream(mediaFile.MediaStorageId.Value))
                {
                    return await stream.ToByteArrayAsync();
                }
            }
            else
            {
                await _db.LoadReferenceAsync(mediaFile, x => x.MediaStorage);
                return mediaFile.MediaStorage?.Data;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task SaveAsync(MediaFile mediaFile, MediaStorageItem item)
            => ApplyBlobAsync(mediaFile, item, true);

        /// <summary>
        /// Applies given media storage <paramref name="item"/> to <paramref name="media"/> entity as blob.
        /// </summary>
        /// <param name="media">Media item</param>
        /// <param name="item">The source item</param>
        /// <param name="save">Whether to commit changes to <paramref name="media"/> entity to database immediately.</param>
        public async Task ApplyBlobAsync(IMediaAware media, MediaStorageItem item, bool save = false)
        {
            Guard.NotNull(media, nameof(media));

            if (item == null)
            {
                save = save && await RemoveInternalAsync(media) == 0;
                media.MediaStorageId = null;
            }
            else
            {
                using (item)
                {
                    await SaveFast(media, item);
                }
            }

            if (save)
            {
                await _db.SaveChangesAsync();
            }
        }

        private async Task<int> SaveFast(IMediaAware media, MediaStorageItem item)
        {
            var sourceStream = item.SourceStream;
            media.Size = (int)sourceStream.Length;

            object blobValue = _db.DataProvider.CanStreamBlob
                ? sourceStream
                : await sourceStream.ToByteArrayAsync();

            var blobParam = _db.DataProvider.CreateParameter("p0", blobValue);

            if (media.MediaStorageId == null)
            {
                // Insert new blob
                var sql = "INSERT INTO MediaStorage (Data) Values(@p0)";
                media.MediaStorageId = await _db.DataProvider.InsertIntoAsync(sql, blobParam);
            }
            else
            {
                // Update existing blob
                var sql = "UPDATE MediaStorage SET Data = @p0 WHERE Id = @p1";
                var idParam = _db.DataProvider.CreateParameter("p1", media.MediaStorageId.Value);
                await _db.Database.ExecuteSqlRawAsync(sql, blobParam, idParam);
            }

            return media.MediaStorageId.Value;
        }

        public virtual async Task RemoveAsync(params MediaFile[] mediaFiles)
        {
            int numRemoved = 0;

            foreach (var media in mediaFiles)
            {
                numRemoved += await RemoveInternalAsync(media);
            }

            if (mediaFiles.Length > 0 && numRemoved < mediaFiles.Length)
            {
                // Not all Blobs have been batch-deleted: commit here.
                await _db.SaveChangesAsync();
            }
        }

        private async Task<int> RemoveInternalAsync(IMediaAware media)
        {
            // Do BatchDelete, we don't wanna load the Blob into memory.
            return await _db.MediaStorage
                .Where(x => x.Id == media.MediaStorageId.Value)
                .BatchDeleteAsync();
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

                // Remove blob from DB with stub entity
                _db.MediaStorage.Remove(new MediaStorage { Id = mediaFile.MediaStorageId.Value });

                mediaFile.MediaStorageId = null;
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
                return ApplyBlobAsync(mediaFile, MediaStorageItem.FromStream(stream), false);
            }

            return Task.CompletedTask;
        }

        Task IMediaSender.OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            if (succeeded && context.AffectedFiles.Any() && _db.DataProvider.CanShrink)
            {
                // Shrink database after sending/removing at least one blob.
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