using System.Diagnostics;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Imaging;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Utility class that helps to load and assign sample media files during
    /// app or module installation.
    /// </summary>
    public class SampleMediaUtility
    {
        private readonly string _rootPath;
        private readonly SmartDbContext _db;
        private readonly IFileSystem _contentRoot;
        private readonly IMediaTypeResolver _mediaTypeResolver;
        private readonly IMediaStorageProvider _storageProvider;

        public SampleMediaUtility(SmartDbContext db, string rootPath)
        {
            _db = Guard.NotNull(db, nameof(db));
            _rootPath = rootPath.EmptyNull();

            var engine = EngineContext.Current;

            _contentRoot = engine.Application.ContentRoot;
            _mediaTypeResolver = engine.ResolveService<IMediaTypeResolver>();
            _storageProvider = engine.ResolveService<Func<IMediaStorageProvider>>().Invoke();
        }

        public async Task<MediaFile> CreateMediaFileAsync(string subpath, string seoFileName = null)
        {
            Guard.NotNull(subpath, nameof(subpath));

            var file = _contentRoot.GetFile(PathUtility.Join(_rootPath, subpath));

            if (!file.Exists)
            {
                throw new FileNotFoundException($"Sample file '{file.SubPath}' does not exist.");
            }

            try
            {
                var ext = file.Extension;
                var mimeType = MimeTypes.MapNameToMimeType(ext);
                var mediaType = _mediaTypeResolver.Resolve(ext, mimeType);
                var now = DateTime.UtcNow;

                var name = seoFileName.HasValue()
                    ? seoFileName.Truncate(100) + ext
                    : file.Name.ToLower().Replace('_', '-');

                var mediaFile = new MediaFile
                {
                    Name = name,
                    MediaType = mediaType,
                    MimeType = mimeType,
                    Extension = ext.EmptyNull().TrimStart('.'),
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now,
                    // So that FolderId is set later during track detection
                    Version = 1
                };

                await ApplyToStorage(file, mediaFile);

                return mediaFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task ApplyToStorage(IFile source, MediaFile mediaFile)
        {
            // Is post-app module installation

            using (var stream = source.OpenRead())
            {
                if (mediaFile.MediaType == MediaType.Image)
                {
                    mediaFile.Size = (int)stream.Length;

                    var pixelSize = ImageHeader.GetPixelSize(stream);
                    if (!pixelSize.IsEmpty)
                    {
                        mediaFile.Width = pixelSize.Width;
                        mediaFile.Height = pixelSize.Height;
                    }
                }

                _db.MediaFiles.Add(mediaFile);
                await _db.SaveChangesAsync();

                await _storageProvider.SaveAsync(mediaFile, MediaStorageItem.FromStream(stream));
            }
        }
    }
}
