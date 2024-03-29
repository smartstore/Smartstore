#nullable enable

using Autofac;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media.Storage
{
    [SystemName("MediaStorage.SmartStoreFileSystem")]
    [FriendlyName("File system")]
    [Order(1)]
    public class FileSystemMediaStorageProvider : IMediaStorageProvider, IMediaSender, IMediaReceiver
    {
        const string MediaRootPath = "Storage";

        private readonly IMediaFileSystem _fileSystem;
        private readonly AsyncRunner _asyncRunner;
        private readonly Dictionary<int, string> _pathCache = [];

        public FileSystemMediaStorageProvider(IMediaFileSystem fileSystem, AsyncRunner asyncRunner)
        {
            _fileSystem = fileSystem;
            _asyncRunner = asyncRunner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public static string SystemName => "MediaStorage.SmartStoreFileSystem";

        protected internal string GetPath(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile);

            if (_pathCache.TryGetValue(mediaFile.Id, out var path))
            {
                return path;
            }

            var ext = mediaFile.Extension.NullEmpty() ?? MimeTypes.MapMimeTypeToExtension(mediaFile.MimeType);

            var fileName = mediaFile.Id.ToString(ImageCache.IdFormatString).Grow(ext, ".");
            var subfolder = fileName[..ImageCache.MaxDirLength];

            path = PathUtility.Join(MediaRootPath, subfolder, fileName);

            _pathCache[mediaFile.Id] = path;

            return path;
        }

        #region IMediaStorageProvider

        public bool IsCloudStorage
            => _fileSystem.IsCloudStorage;

        public virtual async Task<long> GetLengthAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile);

            if (mediaFile.Size > 0)
            {
                return mediaFile.Size;
            }

            var file = await _fileSystem.GetFileAsync(GetPath(mediaFile));
            if (file.Exists)
            {
                // Hopefully a future commit will save this
                mediaFile.Size = (int)file.Length;
            }

            return mediaFile.Size;
        }

        public virtual Stream? OpenRead(MediaFile mediaFile)
        {
            var file = _fileSystem.GetFile(GetPath(Guard.NotNull(mediaFile)));
            return file.Exists ? file.OpenRead() : null;
        }

        public virtual async Task<Stream?> OpenReadAsync(MediaFile mediaFile)
        {
            var file = await _fileSystem.GetFileAsync(GetPath(Guard.NotNull(mediaFile, nameof(mediaFile))));
            return file.Exists ? await file.OpenReadAsync() : null;
        }

        public virtual async Task<byte[]?> LoadAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile);
            return (await _fileSystem.ReadAllBytesAsync(GetPath(mediaFile))) ?? null;
        }

        public virtual async Task SaveAsync(MediaFile mediaFile, MediaStorageItem? item)
        {
            Guard.NotNull(mediaFile);

            // TODO: (?) if the new file extension differs from the old one then the old file never gets deleted

            var filePath = GetPath(mediaFile);

            if (item != null)
            {
                // Create folder if it does not exist yet
                var dir = Path.GetDirectoryName(filePath);
                await _fileSystem.TryCreateDirectoryAsync(dir);

                using (item)
                {
                    using var outStream = await (await _fileSystem.GetFileAsync(filePath)).OpenWriteAsync();
                    await item.SaveToAsync(outStream, mediaFile);
                    //mediaFile.Size = (int)outStream.Length;
                }
            }
            else
            {
                // Remove media storage if any
                await _fileSystem.TryDeleteFileAsync(filePath);
            }
        }

        public virtual async Task RemoveAsync(params MediaFile[] mediaFiles)
        {
            foreach (var media in mediaFiles)
            {
                await _fileSystem.TryDeleteFileAsync(GetPath(media));
            }
        }

        public virtual Task ChangeExtensionAsync(MediaFile mediaFile, string extension)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));
            Guard.NotEmpty(extension, nameof(extension));

            var sourcePath = GetPath(mediaFile);
            var newPath = Path.ChangeExtension(sourcePath, extension);

            return _fileSystem.MoveEntryAsync(sourcePath, newPath);
        }

        #endregion

        #region IMediaSender, IMediaReceiver

        public async Task MoveToAsync(IMediaReceiver target, MediaMoverContext context, MediaFile mediaFile)
        {
            Guard.NotNull(target, nameof(target));
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(mediaFile, nameof(mediaFile));

            var filePath = GetPath(mediaFile);

            try
            {
                // Let target store data (into database for example)
                await target.ReceiveAsync(context, mediaFile, await OpenReadAsync(mediaFile));

                // Remember file path: we must be able to rollback IO operations on transaction failure
                context.AffectedFiles.Add(filePath);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex);
            }
        }

        public async Task ReceiveAsync(MediaMoverContext context, MediaFile mediaFile, Stream stream)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(mediaFile, nameof(mediaFile));

            // store data into file
            if (stream != null)
            {
                using (stream)
                {
                    if (stream.Length > 0)
                    {
                        var filePath = GetPath(mediaFile);

                        if (!await _fileSystem.FileExistsAsync(filePath))
                        {
                            // TBD: (mc) We only save the file if it doesn't exist yet.
                            // This should save time and bandwidth in the case where the target
                            // is a cloud based file system (like Azure BLOB).
                            // In such a scenario it'd be advisable to copy the files manually
                            // with other - maybe more performant - tools before performing the provider switch.

                            // Create directory if it does not exist yet
                            await _fileSystem.TryCreateDirectoryAsync(Path.GetDirectoryName(filePath));

                            await _fileSystem.SaveStreamAsync(filePath, stream, true);

                            context.AffectedFiles.Add(filePath);
                        }
                    }
                }
            }
        }

        Task IMediaSender.OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            return OnCompletedInternal(context, succeeded, cancelToken);
        }

        Task IMediaReceiver.OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            return OnCompletedInternal(context, succeeded, cancelToken);
        }

        private Task OnCompletedInternal(MediaMoverContext context, bool succeeded, CancellationToken cancelToken)
        {
            if (context.AffectedFiles.Count != 0)
            {
                var hasReceived = context.Target == this;

                if ((!hasReceived && succeeded) || (hasReceived && !succeeded))
                {
                    // FS > DB sucessful OR DB > FS failed/aborted: delete all physical files.
                    // Run a background task for the deletion of files (fire & forget)

                    return _asyncRunner.Run(async (scope, ct, state) =>
                    {
                        // Run this fire & forget code in a new scope, because we want
                        // this provider to be disposed as soon as possible.

                        var fileSystem = scope.Resolve<IMediaFileSystem>();
                        var files = state as string[];

                        foreach (var file in files!)
                        {
                            await fileSystem.TryDeleteFileAsync(file);
                        }

                    }, context.AffectedFiles.ToArray(), cancellationToken: cancelToken);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}