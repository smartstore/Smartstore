using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
{
    public class SharpImageFactory : IImageFactory
    {
        public bool IsSupportedImage(string extension)
        {
            return FindInternalImageFormat(extension) != null;
        }

        public IImageFormat FindFormatByExtension(string extension)
        {
            var internalFormat = FindInternalImageFormat(extension);
            if (internalFormat != null)
            {
                return new SharpImageFormat(internalFormat);
            }

            return null;
        }

        public IImageFormat DetectFormat(Stream stream)
        {
            var internalFormat = Image.DetectFormat(stream);
            if (internalFormat != null)
            {
                return new SharpImageFormat(internalFormat);
            }

            return null;
        }

        public async Task<IImageFormat> DetectFormatAsync(Stream stream)
        {
            var internalFormat = await Image.DetectFormatAsync(stream);
            if (internalFormat != null)
            {
                return new SharpImageFormat(internalFormat);
            }

            return null;
        }

        public IImageInfo DetectInfo(Stream stream)
        {
            var info = Image.Identify(stream, out var format);
            if (info != null && format != null)
            {
                return new SharpImageInfo(info, format);
            }

            return null;
        }

        public async Task<IImageInfo> DetectInfoAsync(Stream stream)
        {
            var result = await Image.IdentifyWithFormatAsync(stream);
            var info = result.ImageInfo;
            var format = result.Format;

            if (info != null && format != null)
            {
                return new SharpImageInfo(info, format);
            }

            return null;
        }

        public IProcessableImage Load(string path)
        {
            var image = Image.Load(path, out var format);
            return new SharpImage(image, format);
        }

        public IProcessableImage Load(Stream stream)
        {
            var image = Image.Load(stream, out var format);
            return new SharpImage(image, format);
        }

        public async Task<IProcessableImage> LoadAsync(Stream stream)
        {
            var result = await Image.LoadWithFormatAsync(stream);
            return new SharpImage(result.Image, result.Format);
        }

        private static SixLabors.ImageSharp.Formats.IImageFormat FindInternalImageFormat(string extension)
        {
            if (extension.IsEmpty())
            {
                return null;
            }

            return SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.FindFormatByFileExtension(extension);
        }
    }
}