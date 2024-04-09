using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using Smartstore.Engine;
using Smartstore.Utilities;
using SharpConfiguration = SixLabors.ImageSharp.Configuration;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    public class SharpImageFactory : Disposable, IImageFactory
    {
        private readonly Timer _releaseMemTimer;

        public SharpImageFactory(SmartConfiguration appConfig)
        {
            if (appConfig.ImagingMaxPoolSizeMB > 0)
            {
                SharpConfiguration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
                {
                    MaximumPoolSizeMegabytes = appConfig.ImagingMaxPoolSizeMB
                });
            }

            // Release memory pool every 10 minutes
            var releaseInterval = TimeSpan.FromMinutes(10);
            _releaseMemTimer = new Timer(o => ReleaseMemory(), null, releaseInterval, releaseInterval);
        }

        public bool IsSupportedImage(string extension)
        {
            return FindInternalImageFormat(extension) != null;
        }

        public IImageFormat FindFormatByExtension(string extension)
        {
            var internalFormat = FindInternalImageFormat(extension);
            if (internalFormat != null)
            {
                return ImageSharpUtility.CreateFormat(internalFormat);
            }

            return null;
        }

        public IImageFormat DetectFormat(Stream stream)
        {
            var internalFormat = Image.DetectFormat(stream);
            if (internalFormat != null)
            {
                return ImageSharpUtility.CreateFormat(internalFormat);
            }

            return null;
        }

        public async Task<IImageFormat> DetectFormatAsync(Stream stream)
        {
            var internalFormat = await CommonHelper.TryAction(() => Image.DetectFormatAsync(stream));
            if (internalFormat != null)
            {
                return ImageSharpUtility.CreateFormat(internalFormat);
            }

            return null;
        }

        public IImageInfo DetectInfo(Stream stream)
        {
            var info = CommonHelper.TryAction(() => Image.Identify(stream));
            if (info?.Metadata?.DecodedImageFormat != null)
            {
                return new SharpImageInfo(info);
            }

            return null;
        }

        public async Task<IImageInfo> DetectInfoAsync(Stream stream)
        {
            var info = await CommonHelper.TryAction(() => Image.IdentifyAsync(stream));
            if (info?.Metadata?.DecodedImageFormat != null)
            {
                return new SharpImageInfo(info);
            }

            return null;
        }

        public IProcessableImage Load(string path)
        {
            var image = Image.Load(path);
            return new SharpImage(image);
        }

        public IProcessableImage Load(Stream stream)
        {
            var image = Image.Load(stream);
            return new SharpImage(image);
        }

        public async Task<IProcessableImage> LoadAsync(Stream stream)
        {
            var image = await Image.LoadAsync(stream);
            return new SharpImage(image);
        }

        internal static SixLabors.ImageSharp.Formats.IImageFormat FindInternalImageFormat(string extension)
        {
            if (extension.IsEmpty())
            {
                return null;
            }

            if (SharpConfiguration.Default.ImageFormatsManager.TryFindFormatByFileExtension(extension, out var format))
            {
                return format;
            }

            return null;
        }

        public void ReleaseMemory()
            => SharpConfiguration.Default.MemoryAllocator.ReleaseRetainedResources();

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _releaseMemTimer.Dispose();
        }
    }
}