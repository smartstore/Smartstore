using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Engine;
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
        private readonly bool _appIsInstalled;
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
            _appIsInstalled = engine.Application.IsInstalled;
            _mediaTypeResolver = engine.ResolveService<IMediaTypeResolver>();

            if (_appIsInstalled)
            {
                _storageProvider = engine.ResolveService<Func<IMediaStorageProvider>>().Invoke();
            }
        }

        public async Task<MediaFile> CreateMediaFileAsync(string subpath, string seoFileName = null)
        {
            Guard.NotNull(subpath, nameof(subpath));
            
            var file = _contentRoot.GetFile(_contentRoot.PathCombine(_rootPath, subpath));

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
                    UpdatedOnUtc = now
                };

                var applier = _appIsInstalled
                    ? ApplyToStorage(file, mediaFile)
                    : ApplyToBlob(file, mediaFile);

                await applier;

                return mediaFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task ApplyToBlob(IFile source, MediaFile mediaFile)
        {
            // Is app installation
            
            var buffer = await source.ReadAllBytesAsync();
            mediaFile.Size = buffer.Length;
            mediaFile.MediaStorage = new MediaStorage { Data = buffer };

            // So that FolderId is set later during track detection
            mediaFile.Version = 1;

            if (mediaFile.MediaType == MediaType.Image)
            {
                var pixelSize = ImageHeader.GetPixelSize(buffer, mediaFile.MimeType);
                if (!pixelSize.IsEmpty)
                {
                    mediaFile.Width = pixelSize.Width;
                    mediaFile.Height = pixelSize.Height;
                }
            }
        }

        private async Task ApplyToStorage(IFile source, MediaFile mediaFile)
        {
            // Is post-app module installation

            using (var stream = source.OpenRead())
            {
                if (mediaFile.MediaType == MediaType.Image)
                {
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
