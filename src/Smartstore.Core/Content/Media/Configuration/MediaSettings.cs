using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Configuration;
using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media
{
    public class MediaSettings : ISettings
    {
        private HashSet<int> _allowedThumbSizes;

        public const int ThumbnailSizeXxs = 32;
        public const int ThumbnailSizeXs = 72;
        public const int ThumbnailSizeSm = 128;
        public const int ThumbnailSizeMd = 256;
        public const int ThumbnailSizeLg = 512;
        public const int ThumbnailSizeXl = 680;
        public const int ThumbnailSizeXxl = 1024;
        public const int MaxImageSize = 2048;

        public bool DefaultPictureZoomEnabled { get; set; } = true;
        public string PictureZoomType { get; set; } = "window";

        /// <summary>
        /// Gets or sets the height to width ratio for thumbnails in grid style lists (0.2 - 2)
        /// </summary>
        /// <remarks>
        /// A value greater than 1 indicates, that your product pictures are generally
        /// in portrait format, less than 1 indicates landscape format.
        /// </remarks>
        public float DefaultThumbnailAspectRatio { get; set; } = 1;

        /// <summary>
        /// Geta or sets a vaue indicating whether single (/media/thumbs/) or multiple (/media/thumbs/0001/ and /media/thumbs/0002/) directories will used for picture thumbs
        /// </summary>
        public bool MultipleThumbDirectories { get; set; } = true;

        /// <summary>
        /// Generates absolute media urls based upon current request uri instead of relative urls.
        /// </summary>
        public bool AutoGenerateAbsoluteUrls { get; set; } = true;

        /// <summary>
        /// Whether orphaned files should automatically be marked as transient so that the daily cleanup task may delete them.
        /// </summary>
        public bool MakeFilesTransientWhenOrphaned { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum size (in KB) of an uploaded media file. The default is 102,400 (100 MB).
        /// </summary>
        public long MaxUploadFileSize { get; set; } = 102400;

        #region Thumb sizes / security

        public int AvatarPictureSize { get; set; } = ThumbnailSizeMd;
        public int ProductThumbPictureSize { get; set; } = ThumbnailSizeMd;
        public int ProductDetailsPictureSize { get; set; } = ThumbnailSizeXl;
        public int ProductThumbPictureSizeOnProductDetailsPage { get; set; } = ThumbnailSizeXs;
        public int MessageProductThumbPictureSize { get; set; } = ThumbnailSizeXs;
        public int AssociatedProductPictureSize { get; set; } = ThumbnailSizeXl;
        public int AssociatedProductHeaderThumbSize { get; set; } = ThumbnailSizeXxs;
        public int BundledProductPictureSize { get; set; } = ThumbnailSizeXs;
        public int CategoryThumbPictureSize { get; set; } = ThumbnailSizeMd;
        public int ManufacturerThumbPictureSize { get; set; } = ThumbnailSizeMd;
        public int CartThumbPictureSize { get; set; } = ThumbnailSizeMd;
        public int CartThumbBundleItemPictureSize { get; set; } = ThumbnailSizeXxs;
        public int MiniCartThumbPictureSize { get; set; } = ThumbnailSizeMd;
        public int VariantValueThumbPictureSize { get; set; } = ThumbnailSizeXs;
        public int AttributeOptionThumbPictureSize { get; set; } = ThumbnailSizeXs;

        public List<int> AllowedExtraThumbnailSizes { get; set; }

        public int MaximumImageSize { get; set; } = MaxImageSize;

        public int[] GetAllowedThumbnailSizes()
        {
            EnsureThumbSizeWhitelist();
            return _allowedThumbSizes.ToArray();
        }

        public bool IsAllowedThumbnailSize(int size)
        {
            EnsureThumbSizeWhitelist();
            return _allowedThumbSizes.Contains(size);
        }

        public int GetNextValidThumbnailSize(int currentSize)
        {
            EnsureThumbSizeWhitelist();

            if (_allowedThumbSizes.Contains(currentSize))
            {
                return currentSize;
            }

            foreach (var size in _allowedThumbSizes)
            {
                if (size >= currentSize)
                {
                    return size;
                }
            }

            return MaxImageSize;
        }

        private void EnsureThumbSizeWhitelist()
        {
            if (_allowedThumbSizes != null)
            {
                return;
            }

            var numExtraSizes = AllowedExtraThumbnailSizes?.Count ?? 0;

            var sizes = new int[]
            {
                48, ThumbnailSizeXxs, ThumbnailSizeXs, ThumbnailSizeSm, ThumbnailSizeMd, ThumbnailSizeLg, ThumbnailSizeXl, ThumbnailSizeXxl,
                AvatarPictureSize,
                ProductThumbPictureSize,
                ProductDetailsPictureSize,
                ProductThumbPictureSizeOnProductDetailsPage,
                MessageProductThumbPictureSize,
                AssociatedProductPictureSize,
                BundledProductPictureSize,
                CategoryThumbPictureSize,
                ManufacturerThumbPictureSize,
                CartThumbPictureSize,
                CartThumbBundleItemPictureSize,
                MiniCartThumbPictureSize,
                VariantValueThumbPictureSize,
                AttributeOptionThumbPictureSize,
                MaxImageSize
            }
            .Concat(numExtraSizes > 0 ? AllowedExtraThumbnailSizes : Enumerable.Empty<int>())
            .OrderBy(x => x);

            _allowedThumbSizes = new HashSet<int>(sizes);
        }

        #endregion

        #region Media types

        /// <summary>
        /// A space separated list of image type file extensions (dotless)
        /// </summary>
        public string ImageTypes { get; set; }

        /// <summary>
        /// A space separated list of video type file extensions (dotless)
        /// </summary>
        public string VideoTypes { get; set; }

        /// <summary>
        /// A space separated list of audio type file extensions (dotless)
        /// </summary>
        public string AudioTypes { get; set; }

        /// <summary>
        /// A space separated list of document type file extensions (dotless)
        /// </summary>
        public string DocumentTypes { get; set; }

        /// <summary>
        /// A space separated list of text type file extensions (dotless)
        /// </summary>
        public string TextTypes { get; set; }

        /// <summary>
        /// A space separated list of other types file extensions (dotless)
        /// </summary>
        public string BinTypes { get; set; }

        #endregion

        #region Image processing

        /// <summary>
        /// Gets or sets the default resampling mode during image resize operations.
        /// </summary>
        public ResamplingMode DefaultResamplingMode { get; set; } = ResamplingMode.Bicubic;

        /// <summary>
        /// Gets or sets the default JPEG quality used for JPEG encoding.
        /// </summary>
        public int DefaultImageQuality { get; set; } = 85;

        /// <summary>
        /// Gets or sets the default JPEG subsampling used for JPEG encoding.
        /// </summary>
        public JpegColorType? JpegColorType { get; set; }

        /// <summary>
        /// Gets or sets the default compression level used for PNG encoding.
        /// </summary>
        public PngCompressionLevel PngCompressionLevel { get; set; } = PngCompressionLevel.BestCompression;

        /// <summary>
        /// Gets or sets the default quantization method used for PNG encoding.
        /// </summary>
        public QuantizationMethod PngQuantizationMethod { get; set; } = QuantizationMethod.Wu;

        /// <summary>
        /// Whether PNG should be encoded interlaced.
        /// </summary>
        public bool PngInterlaced { get; set; } = true;

        /// <summary>
        /// Whether PNG metadata should be ignored when the image is being encoded.
        /// </summary>
        public bool PngIgnoreMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets the default quantization method used for GIF encoding.
        /// </summary>
        public QuantizationMethod GifQuantizationMethod { get; set; } = QuantizationMethod.Octree;

        /// <summary>
        /// Finds embedded Base64 images in long HTML descriptions, extracts and saves them
        /// to the media storage, and replaces the Base64 fragment with the media path.
        /// Offloading is automatically triggered by saving an entity to the database.
        /// Currently supported entity types are: 
        /// <c>Product</c>, <c>Category</c>, <c>Manufacturer</c> and <c>Topic</c>.
        /// </summary>
        public bool OffloadEmbeddedImagesOnSave { get; set; } = true;

        #endregion

        #region Response Caching

        /// <summary>
        /// Gets or sets the duration in seconds for which a media file response is cached.
        /// This sets "max-age" in "Cache-control" header.
        /// The default is 604800 sec. (7 days).
        /// </summary>
        public int ResponseCacheDuration { get; set; } = 604800;

        /// <summary>
        /// Gets or sets the location where a media file response must be cached.
        /// The defualt is <see cref="ResponseCacheLocation.Any"/>
        /// (cache in both proxies and client, sets "Cache-control" header to "public").
        /// </summary>
        public ResponseCacheLocation ResponseCacheLocation { get; set; } = ResponseCacheLocation.Any;

        /// <summary>
        /// Gets or sets the value which determines whether a media file response should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "duration" parameter.
        /// Setting this to <see langword="true"/> has the effect that no HTTP 304 revalidation can
        /// occur on the server because the client has been instructed not to cache the response.
        /// </summary>
        public bool ResponseCacheNoStore { get; set; }

        /// <summary>
        /// If <see langword="true"/>, appends file version hash to every generated url in "ver" query
        /// to force clients (e.g. the browser) to ignore their local copy when the file has changed.
        /// The hash includes last modified date and file size.
        /// </summary>
        /// <remarks>
        /// Setting this to <see langword="true"/> does not make any sense when the cache location
        /// is <see cref="ResponseCacheLocation.None"/> or <see cref="ResponseCacheNoStore"/> is <see langword="true"/>.
        /// </remarks>
        public bool AppendFileVersionToUrl { get; set; }

        #endregion
    }
}