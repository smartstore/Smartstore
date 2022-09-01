using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImageInfo : IImageInfo
    {
        private readonly SixLabors.ImageSharp.IImageInfo _info;
        private readonly IImageFormat _format;

        public SharpImageInfo(SixLabors.ImageSharp.IImageInfo info, SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            _info = info;
            _format = ImageSharpUtility.CreateFormat(format);
        }

        public int Width
            => _info.Width;

        public int Height
            => _info.Height;

        public byte BitDepth
            => (byte)(_info.PixelType?.BitsPerPixel);

        public IImageFormat Format
            => _format;

        public IEnumerable<ImageMetadataEntry> GetMetadata()
        {
            return ConvertMetadata(_info.Metadata);
        }

        public static IEnumerable<ImageMetadataEntry> ConvertMetadata(ImageMetadata metadata)
        {
            if (metadata == null)
            {
                yield break;
            }
            
            var iptcValues = metadata.IptcProfile?.Values;
            if (iptcValues != null)
            {
                foreach (var entry in iptcValues)
                {
                    if (entry.Tag > IptcTag.Unknown)
                    {
                        yield return new ImageMetadataEntry(entry.Tag.ToString(), entry.Value, ImageMetadataProfile.Iptc);
                    }
                }
            }

            var exifValues = metadata.ExifProfile?.Values;
            if (exifValues != null && exifValues.Count > 0)
            {
                foreach (var entry in exifValues)
                {
                    yield return new ImageMetadataEntry(entry.Tag.ToString(), entry.ToString(), ImageMetadataProfile.Exif);
                }
            }
        }
    }
}
