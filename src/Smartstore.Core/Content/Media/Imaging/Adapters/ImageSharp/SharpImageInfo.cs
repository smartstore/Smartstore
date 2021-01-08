using System;

namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImageInfo : IImageInfo
    {
        private readonly SixLabors.ImageSharp.IImageInfo _info;
        private readonly IImageFormat _format;

        public SharpImageInfo(SixLabors.ImageSharp.IImageInfo info, SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            _info = info;
            _format = new SharpImageFormat(format);
        }

        public int Width 
            => _info.Width;

        public int Height 
            => _info.Height;

        public byte BitDepth 
            => (byte)(_info.PixelType?.BitsPerPixel);

        public IImageFormat Format 
            => _format;
    }
}
