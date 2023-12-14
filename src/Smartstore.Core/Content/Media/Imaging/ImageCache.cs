using System.Runtime.CompilerServices;
using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    public class ImageCache : IImageCache
    {
        public const string IdFormatString = "0000000";
        internal const int MaxDirLength = 4;

        private readonly MediaSettings _mediaSettings;
        private readonly IMediaFileSystem _fileSystem;
        private readonly string _thumbsRootDir;

        public ImageCache(MediaSettings mediaSettings, IMediaFileSystem fileSystem)
        {
            _mediaSettings = mediaSettings;
            _fileSystem = fileSystem;
            _thumbsRootDir = "Thumbs/";
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task PutAsync(CachedImage cachedImage, IImage image)
        {
            var path = BuildPath(cachedImage.Path);
            using var stream = await (await _fileSystem.GetFileAsync(path)).OpenWriteAsync();

            if (await PreparePut(cachedImage, stream))
            {
                await image.SaveAsync(stream);
                image.Dispose();
                await PostPut(cachedImage, path);
            }
        }

        public virtual async Task PutAsync(CachedImage cachedImage, Stream stream)
        {
            if (await PreparePut(cachedImage, stream))
            {
                var path = BuildPath(cachedImage.Path);
                await _fileSystem.SaveStreamAsync(path, stream);
                await PostPut(cachedImage, path);
            }
        }

        private async Task<bool> PreparePut(CachedImage cachedImage, Stream stream)
        {
            Guard.NotNull(cachedImage, nameof(cachedImage));

            if (stream == null)
            {
                return false;
            }

            // create folder if needed
            string imageDir = System.IO.Path.GetDirectoryName(cachedImage.Path);
            if (imageDir.HasValue())
            {
                await _fileSystem.TryCreateDirectoryAsync(BuildPath(imageDir));
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task PostPut(CachedImage cachedImage, string path)
        {
            cachedImage.Exists = true;
            cachedImage.File = await _fileSystem.GetFileAsync(path);
        }

        public virtual async Task<CachedImage> GetAsync(int? mediaFileId, MediaPathData pathData, ProcessImageQuery query = null)
        {
            Guard.NotNull(pathData, nameof(pathData));

            var resultExtension = query?.GetResultExtension();
            if (resultExtension != null)
            {
                pathData.Extension = resultExtension;
            }

            var imagePath = GetCachedImagePath(mediaFileId, pathData, query);
            var file = await _fileSystem.GetFileAsync(BuildPath(imagePath));

            var result = new CachedImage(file)
            {
                Path = imagePath,
                Extension = pathData.Extension,
                IsRemote = _fileSystem.IsCloudStorage
            };

            return result;
        }

        public virtual async Task RefreshInfoAsync(CachedImage cachedImage)
        {
            Guard.NotNull(cachedImage, nameof(cachedImage));

            var file = await _fileSystem.GetFileAsync(cachedImage.File.SubPath);
            cachedImage.File = file;
            cachedImage.Exists = file.Exists;
        }

        public virtual async Task DeleteAsync(MediaFile mediaFile)
        {
            Guard.NotNull(mediaFile, nameof(mediaFile));

            var filter = string.Format("{0}*.*", mediaFile.Id.ToString(IdFormatString));

            foreach (var file in await _fileSystem.EnumerateFilesAsync(_thumbsRootDir, filter, deep: true).AsyncToArray())
            {
                await file.DeleteAsync();
            }
        }

        public virtual async Task ClearAsync()
        {
            if (await _fileSystem.TryDeleteDirectoryAsync(_thumbsRootDir))
            {
                await _fileSystem.TryCreateDirectoryAsync(_thumbsRootDir);
            }
        }

        public virtual async Task<(long fileCount, long totalSize)> CacheStatisticsAsync()
        {
            long fileCount = 0;
            long totalSize = 0;

            if (!await _fileSystem.DirectoryExistsAsync(_thumbsRootDir))
            {
                return (0, 0);
            }

            fileCount = await _fileSystem.CountFilesAsync(_thumbsRootDir, deep: true);
            totalSize = await _fileSystem.GetDirectorySizeAsync(_thumbsRootDir);

            return (fileCount, totalSize);
        }

        #region Utils

        protected string GetCachedImagePath(int? mediaFileId, MediaPathData data, ProcessImageQuery query = null)
        {
            string result = "";

            // xxxxxxx
            if (mediaFileId.GetValueOrDefault() > 0)
            {
                result = mediaFileId.Value.ToString(IdFormatString);
            }

            //// INFO: (mm) don't include folder id in pathes for now. It results in more complex image cache invalidation code.
            //// xxxxxxx-f
            //if (data.Folder != null)
            //{
            //	result = result.Grow(data.Folder.Id.ToString(CultureInfo.InvariantCulture), "-");
            //}

            // xxxxxxx-f-abc
            result = result.Grow(data.FileTitle, "-");

            if (result.IsEmpty())
            {
                // files without name? No way!
                return null;
            }

            if (query != null && query.NeedsProcessing())
            {
                // xxxxxxx-f-abc-w100-h100
                result += query.CreateHash();
            }

            if (_mediaSettings.MultipleThumbDirectories && result.Length > MaxDirLength)
            {
                // Get the first four letters of the file name
                // 0001/xxxxxxx-f-abc-w100-h100
                var subDirectoryName = result.Substring(0, MaxDirLength);
                result = subDirectoryName + "/" + result;
            }

            // 0001/xxxxxxx-f-abc-w100-h100.png
            return result.Grow(data.Extension, ".");
        }

        private string BuildPath(string imagePath)
        {
            if (imagePath.IsEmpty())
                return null;

            return _thumbsRootDir + imagePath;
        }

        #endregion
    }
}
